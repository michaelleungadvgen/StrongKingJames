using StrongKingJames.Core.Models;
using StrongKingJames.Core.Notes;
using StrongKingJames.Core.Search;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Web.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/books", (IBibleRepository repo, CancellationToken ct) => repo.GetBooksAsync(ct));

        api.MapGet("/books/{abbrev}/chapters/{ch:int}",
            (string abbrev, int ch, IBibleRepository repo, CancellationToken ct) => repo.GetChapterAsync(abbrev, ch, ct));

        api.MapGet("/strongs/{number}", async (string number, IBibleRepository repo, CancellationToken ct) =>
            await repo.GetStrongsEntryAsync(number, ct) is { } e ? Results.Ok(e) : Results.NotFound());

        api.MapGet("/strongs/{number}/verses",
            (string number, IBibleRepository repo, CancellationToken ct) => repo.GetVersesByStrongsAsync(number, ct));

        api.MapGet("/search", async (string q, string? mode, int? bookId, string? testament,
            IBibleRepository repo, ISearchService search, IEmbeddingService embed, CancellationToken ct) =>
        {
            var m = Enum.TryParse<SearchMode>(mode, true, out var parsed) ? parsed : SearchMode.Auto;
            if (m == SearchMode.Auto) m = SearchModeDetector.Detect(q);
            return m switch
            {
                SearchMode.Strongs => Results.Ok(await repo.GetVersesByStrongsAsync(q.Trim(), ct)),
                SearchMode.Keyword => Results.Ok(await repo.KeywordSearchAsync(q, 100, bookId, testament, ct)),
                SearchMode.Semantic => Results.Ok(await search.SemanticSearchAsync(await embed.EmbedAsync(q, ct), 20, bookId, testament, ct)),
                // Reference navigation is a UI concern; the API returns an empty list for reference-mode queries.
                _ => Results.Ok(Array.Empty<SearchResult>())
            };
        });

        // Notes CRUD (used by the UI's NoteService directly, and exposed for future clients).
        api.MapGet("/notes", (NoteService notes, CancellationToken ct) => notes.GetAllAsync(ct));

        api.MapPost("/notes", async (Note note, NoteService notes, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(note.Title))
                return Results.BadRequest("Title is required.");
            var saved = await notes.SaveAsync(note, ct);
            return Results.Ok(saved);
        });

        api.MapDelete("/notes/{id:int}", async (int id, NoteService notes, CancellationToken ct) =>
        {
            await notes.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // Single streaming chat endpoint (Server-Sent Events).
        api.MapPost("/chat", async (ChatRequest body, IRagService rag, HttpContext ctx) =>
        {
            if (string.IsNullOrWhiteSpace(body.Question))
                return Results.BadRequest("Question is required.");
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            await foreach (var chunk in rag.AnswerAsync(
                body.Question, body.Model, includeNotes: body.IncludeNotes, ct: ctx.RequestAborted))
            {
                // Keep multi-line content valid: a newline in a chunk starts a new SSE data line within the same event.
                await ctx.Response.WriteAsync($"data: {chunk.Replace("\n", "\ndata: ")}\n\n", ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
            return Results.Empty;
        });
    }

    public record ChatRequest(string Question, string? Model = null, bool IncludeNotes = false);
}
