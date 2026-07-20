using StrongKingJames.Core.Models;
using StrongKingJames.Core.Search;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Web.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/books", (IBibleRepository repo) => repo.GetBooksAsync());

        api.MapGet("/books/{abbrev}/chapters/{ch:int}",
            (string abbrev, int ch, IBibleRepository repo) => repo.GetChapterAsync(abbrev, ch));

        api.MapGet("/strongs/{number}", async (string number, IBibleRepository repo) =>
            await repo.GetStrongsEntryAsync(number) is { } e ? Results.Ok(e) : Results.NotFound());

        api.MapGet("/strongs/{number}/verses",
            (string number, IBibleRepository repo) => repo.GetVersesByStrongsAsync(number));

        api.MapGet("/search", async (string q, string? mode,
            IBibleRepository repo, ISearchService search, IEmbeddingService embed) =>
        {
            var m = Enum.TryParse<SearchMode>(mode, true, out var parsed) ? parsed : SearchMode.Auto;
            if (m == SearchMode.Auto) m = SearchModeDetector.Detect(q);
            return m switch
            {
                SearchMode.Strongs => Results.Ok(await repo.GetVersesByStrongsAsync(q.Trim())),
                SearchMode.Semantic => Results.Ok(await search.SemanticSearchAsync(await embed.EmbedAsync(q), 20)),
                // Reference navigation is a UI concern; the API returns an empty list for reference-mode queries.
                _ => Results.Ok(Array.Empty<SearchResult>())
            };
        });

        // Single streaming chat endpoint (Server-Sent Events).
        api.MapPost("/chat", async (ChatRequest body, IRagService rag, HttpContext ctx) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            await foreach (var chunk in rag.AnswerAsync(body.Question, ctx.RequestAborted))
            {
                // Keep multi-line content valid: a newline in a chunk starts a new SSE data line within the same event.
                await ctx.Response.WriteAsync($"data: {chunk.Replace("\n", "\ndata: ")}\n\n", ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
        });
    }

    public record ChatRequest(string Question);
}
