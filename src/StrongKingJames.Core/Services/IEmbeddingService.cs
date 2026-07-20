using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
