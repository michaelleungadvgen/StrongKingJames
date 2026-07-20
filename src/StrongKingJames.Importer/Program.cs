using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StrongKingJames.Core.Ollama;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Importer;
using StrongKingJames.Importer.Seeding;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var options = ImporterOptions.Parse(args, builder.Configuration);

builder.Services.AddBibleData(options.ConnectionString);
builder.Services.AddSingleton(new OllamaOptions { BaseUrl = options.OllamaBaseUrl, EmbeddingModel = options.EmbeddingModel });
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();

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
Console.WriteLine("Seeding verses...");
await seeder.SeedVersesAsync(options.OsisPath);
Console.WriteLine("Seeding Strong's entries...");
await seeder.SeedStrongsAsync(options.HebrewPath, options.GreekPath);

Console.WriteLine("Backfilling embeddings...");
var backfiller = new EmbeddingBackfiller(db, embedder);
var count = await backfiller.RunAsync(new Progress<int>(n => { if (n % 100 == 0) Console.WriteLine($"  embedded {n}..."); }));

var bookCount = await db.Books.CountAsync();
var verseCount = await db.Verses.CountAsync();
var verseWordCount = await db.VerseWords.CountAsync();
var strongsCount = await db.StrongsEntries.CountAsync();

Console.WriteLine($"Done. Books={bookCount} Verses={verseCount} " +
                  $"VerseWords={verseWordCount} Strongs={strongsCount} Embeddings(new)={count}");

if (bookCount != 66)
    Console.WriteLine($"WARNING: expected 66 books but found {bookCount} — import may be incomplete.");
if (verseCount != 31102)
    Console.WriteLine($"WARNING: expected 31102 KJV verses but found {verseCount} — import may be incomplete (check the OSIS source file).");
