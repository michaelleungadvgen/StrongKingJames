using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(
        float[] queryEmbedding,
        int topK,
        int? bookId = null,
        string? testament = null,
        CancellationToken ct = default);
}
