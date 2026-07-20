using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StrongKingJames.Core.Ollama;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Importer;
using StrongKingJames.Importer.Parsing;
using StrongKingJames.Importer.Seeding;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddServiceDefaults();

    var options = ImporterOptions.Parse(args, builder.Configuration);

    Console.WriteLine($"Importer options: Format={options.Format}, Bible={options.KjsBiblePath}, Strongs={options.KjsStrongsPath}, Dict={options.KjsStrongsDictPath}");
    Console.WriteLine($"Connection={options.ConnectionString}, Ollama={options.OllamaBaseUrl}");

    builder.Services.AddBibleData(options.ConnectionString);
    builder.Services.AddSingleton(new OllamaOptions { BaseUrl = options.OllamaBaseUrl, EmbeddingModel = options.EmbeddingModel });
    // Long timeout + no standard resilience handler: the first embedding call can block while
    // Ollama loads the model, which exceeds the 10s default from AddServiceDefaults.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental but stable enough for our use
    builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>()
        .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(5))
        .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<BibleDbContext>();
    var embedder = sp.GetRequiredService<IEmbeddingService>();

    Console.WriteLine("Applying migrations...");
    await db.Database.MigrateAsync();

    var seeder = new DatabaseSeeder(db);
    Console.WriteLine("Seeding books...");
    await seeder.SeedBooksAsync();

    var format = (options.Format ?? "osis").Trim().ToLowerInvariant();
    Console.WriteLine($"Seeding verses (format={format})...");
    if (format == "kjs")
    {
        var kjs = new KjsJsonParser();
        await seeder.SeedVersesAsync(kjs.Parse(options.KjsBiblePath, options.KjsStrongsPath));
        Console.WriteLine("Seeding Strong's entries (kjs JSON lexicon)...");
        await seeder.SeedStrongsFromJsonAsync(options.KjsStrongsDictPath);
    }
    else
    {
        var osis = new OsisParser();
        await seeder.SeedVersesAsync(osis.Parse(options.OsisPath));
        Console.WriteLine("Seeding Strong's entries (OSIS lexicons)...");
        await seeder.SeedStrongsAsync(options.HebrewPath, options.GreekPath);
    }

    Console.WriteLine("Backfilling embeddings...");
    var backfiller = new EmbeddingBackfiller(db, embedder);
    var count = await backfiller.RunAsync(new Progress<int>(n => { if (n % 100 == 0) Console.WriteLine($"  embedded {n}..."); }));

    var bookCount = await db.Books.CountAsync();
    var verseCount = await db.Verses.CountAsync();
    var verseWordCount = await db.VerseWords.CountAsync();
    var strongsCount = await db.StrongsEntries.CountAsync();
    var taggedWordCount = await db.VerseWords.CountAsync(w => w.StrongsNumber != null);

    Console.WriteLine($"Done. Books={bookCount} Verses={verseCount} " +
                      $"VerseWords={verseWordCount} (tagged={taggedWordCount}) " +
                      $"Strongs={strongsCount} Embeddings(new)={count}");

    if (bookCount != 66)
        Console.WriteLine($"WARNING: expected 66 books but found {bookCount} — import may be incomplete.");
    if (verseCount != 31102)
        Console.WriteLine($"WARNING: expected 31102 KJV verses but found {verseCount} — import may be incomplete (check the source files).");
    if (strongsCount == 0)
        Console.WriteLine("WARNING: 0 Strong's entries — the lexicon(s) did not parse. Check the dictionary source files.");
    else if (strongsCount < 13000)
        Console.WriteLine($"WARNING: only {strongsCount} Strong's entries (expected ~14298: ~8674 Hebrew + ~5624 Greek) — a lexicon may be missing or truncated.");
    if (taggedWordCount == 0)
        Console.WriteLine("WARNING: no Strong's tags parsed — check the OSIS source. " +
                          "(For a non-tagged Bible source use a Strong's-tagged one, e.g. 1John419/kjs JSON via --format kjs.)");
}
catch (Exception ex)
{
    var diag = $"FATAL: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";
    Console.WriteLine(diag);
    try
    {
        var crashLog = Path.Combine(AppContext.BaseDirectory, "importer-crash.log");
        File.WriteAllText(crashLog, diag + "\n");
        Console.WriteLine($"Crash details written to: {crashLog}");
    }
    catch { }
    throw;
}
