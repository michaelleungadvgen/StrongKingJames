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
            var hasEmbed = names.Any(n => n.StartsWith(options.EmbeddingModel, StringComparison.OrdinalIgnoreCase));
            var hasChat = names.Any(n => n.StartsWith(options.ChatModel, StringComparison.OrdinalIgnoreCase));
            if (hasEmbed && hasChat) return (true, true, null);
            var missing = new[] { hasEmbed ? null : options.EmbeddingModel, hasChat ? null : options.ChatModel }
                .Where(x => x is not null);
            var message = "Missing Ollama models. Run: " + string.Join("  &&  ", missing.Select(m => $"ollama pull {m}"));
            return (true, false, message);
        }
        catch (Exception ex)
        {
            return (false, false, $"Cannot reach Ollama at {options.BaseUrl}: {ex.Message}");
        }
    }

    /// <summary>Returns the names of models installed on the Ollama host (empty if unreachable).</summary>
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"{options.BaseUrl}/api/tags", ct);
            if (!resp.IsSuccessStatusCode) return [];
            var tags = await resp.Content.ReadFromJsonAsync<TagsResponse>(ct);
            return tags?.Models?.Select(m => m.Name).OrderBy(n => n).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private record TagsResponse(List<ModelInfo>? Models);
    private record ModelInfo(string Name);
}
