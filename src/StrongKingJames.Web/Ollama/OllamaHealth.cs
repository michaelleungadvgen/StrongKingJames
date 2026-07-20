using System.Net.Http.Json;
using StrongKingJames.Core.Ollama;

namespace StrongKingJames.Web.Ollama;

public class OllamaHealth(HttpClient http, OllamaOptions options)
{
    public async Task<(bool Reachable, bool ModelsReady, string? Message)> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"{options.BaseUrl}/api/tags", ct);
            if (!resp.IsSuccessStatusCode) return (false, false, $"Ollama returned {(int)resp.StatusCode}.");
            var tags = await resp.Content.ReadFromJsonAsync<TagsResponse>(ct);
            var names = tags?.Models?.Select(m => m.Name).ToList() ?? [];
            var hasEmbed = names.Any(n => n.StartsWith(options.EmbeddingModel));
            var hasChat = names.Any(n => n.StartsWith(options.ChatModel));
            if (hasEmbed && hasChat) return (true, true, null);
            var missing = string.Join(", ",
                new[] { hasEmbed ? null : options.EmbeddingModel, hasChat ? null : options.ChatModel }
                .Where(x => x is not null));
            return (true, false, $"Run: ollama pull {missing}");
        }
        catch (Exception ex)
        {
            return (false, false, $"Cannot reach Ollama at {options.BaseUrl}: {ex.Message}");
        }
    }

    private record TagsResponse(List<ModelInfo>? Models);
    private record ModelInfo(string Name);
}
