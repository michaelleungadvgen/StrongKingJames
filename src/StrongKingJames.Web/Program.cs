using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Ollama;
using StrongKingJames.Core.Rag;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Web.Components;
using StrongKingJames.Web.Endpoints;
using StrongKingJames.Web.Ollama;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var bibleConn = builder.Configuration.GetConnectionString("bible")
    ?? throw new InvalidOperationException(
        "Connection string 'bible' not found. It is injected by the Aspire AppHost; " +
        "to run the web app standalone set the ConnectionStrings__bible environment variable.");
builder.Services.AddBibleData(bibleConn);

var ollama = builder.Configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new();
builder.Services.AddSingleton(ollama);

// The Ollama clients talk to a local LLM: responses (especially streaming chat) routinely
// take far longer than the 10s default of the standard resilience handler that
// AddServiceDefaults applies to every HttpClient — which otherwise aborts them with a
// Polly TimeoutRejectedException. Remove that resilience handler for these clients and use
// a long/no timeout. Resilience retries also buffer responses, which would break streaming.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental but stable enough for our use
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(5))
    .RemoveAllResilienceHandlers();
builder.Services.AddHttpClient<IChatService, OllamaChatService>()
    .ConfigureHttpClient(c => c.Timeout = Timeout.InfiniteTimeSpan)
    .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
builder.Services.AddHttpClient<OllamaHealth>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<StrongKingJames.Core.Notes.NoteService>();

// Feature toggles (e.g. Features:NotesEnabled).
var features = builder.Configuration.GetSection("Features").Get<StrongKingJames.Web.Configuration.FeatureOptions>() ?? new();
builder.Services.AddSingleton(features);

// Serialize/accept enums (e.g. NoteType) as strings in the minimal-API JSON.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

// Ensure the database schema is current (applies pending migrations such as the notes
// tables). The web app self-migrates so it works against any database it points at, not
// only ones the importer has already migrated.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StrongKingJames.Data.BibleDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapApiEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
