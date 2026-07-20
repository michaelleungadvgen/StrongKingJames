using System.Net.Http.Json;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Ollama;

public class OllamaEmbeddingService(HttpClient http, OllamaOptions options) : IEmbeddingService
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(
            $"{options.BaseUrl}/api/embeddings",
            new { model = options.EmbeddingModel, prompt = text }, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return body!.Embedding;
    }

    private record EmbeddingResponse(float[] Embedding);
}
