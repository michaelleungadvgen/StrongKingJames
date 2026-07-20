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

var app = builder.Build();

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
