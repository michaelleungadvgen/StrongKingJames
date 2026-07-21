using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Notes;

/// <summary>
/// Orchestrates note persistence and embedding: every save embeds the note text via Ollama
/// so notes can be retrieved semantically in Chat and Search.
/// </summary>
public class NoteService(INoteRepository repo, IEmbeddingService embedder)
{
    public Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default) => repo.GetAllAsync(ct);

    public Task<Note?> GetAsync(int id, CancellationToken ct = default) => repo.GetAsync(id, ct);

    public async Task<Note> SaveAsync(Note note, CancellationToken ct = default)
    {
        note.Title = note.Title.Trim();
        note.Body = note.Body.Trim();

        var saved = note.Id == 0
            ? await repo.AddAsync(note, ct)
            : await UpdateAndReturnAsync(note, ct);

        // Embed the note so it participates in semantic retrieval. If Ollama is unavailable,
        // the note is still saved; it just won't be searchable until re-saved.
        try
        {
            var vector = await embedder.EmbedAsync(saved.ToEmbeddingText(), ct);
            await repo.SetEmbeddingAsync(saved.Id, vector, ct);
        }
        catch
        {
            // swallow — persistence succeeded; embedding is best-effort.
        }

        return saved;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default) => repo.DeleteAsync(id, ct);

    /// <summary>Semantic search over notes for a free-text query.</summary>
    public async Task<IReadOnlyList<NoteSearchResult>> SearchAsync(
        string query, int topK, CancellationToken ct = default)
    {
        var vector = await embedder.EmbedAsync(query, ct);
        return await repo.SemanticSearchNotesAsync(vector, topK, ct);
    }

    private async Task<Note> UpdateAndReturnAsync(Note note, CancellationToken ct)
    {
        await repo.UpdateAsync(note, ct);
        return note;
    }
}
