using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default);
}
