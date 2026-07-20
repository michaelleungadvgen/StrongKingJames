using StrongKingJames.Core.Ollama;
using StrongKingJames.Core.Rag;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Web.Components;
using StrongKingJames.Web.Ollama;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBibleData(builder.Configuration.GetConnectionString("bible")!);

var ollama = builder.Configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new();
builder.Services.AddSingleton(ollama);
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddHttpClient<IChatService, OllamaChatService>();
builder.Services.AddHttpClient<OllamaHealth>();
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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
