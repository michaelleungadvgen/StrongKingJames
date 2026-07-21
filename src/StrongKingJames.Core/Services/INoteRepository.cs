using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface INoteRepository
{
    Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default);
    Task<Note?> GetAsync(int id, CancellationToken ct = default);
    Task<Note> AddAsync(Note note, CancellationToken ct = default);
    Task UpdateAsync(Note note, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Store (or replace) the embedding vector for a note.</summary>
    Task SetEmbeddingAsync(int noteId, float[] embedding, CancellationToken ct = default);

    /// <summary>Nearest notes to the query embedding (cosine), closest first.</summary>
    Task<IReadOnlyList<NoteSearchResult>> SemanticSearchNotesAsync(
        float[] queryEmbedding, int topK, CancellationToken ct = default);
}
