using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Ollama;

public class OllamaChatService(HttpClient http, OllamaOptions options) : IChatService
{
    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new
        {
            model = options.ChatModel,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl}/api/chat")
        {
            Content = JsonContent.Create(request),
        };
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, leaveOpen: true);
        // Null-sentinel loop rather than `while (!reader.EndOfStream)`: CA2024 forbids
        // EndOfStream in async methods (it blocks). Ollama streams NDJSON, one JSON object
        // per line; the final line is {"done":true} with no message.content, so it's skipped.
        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("message", out var msg)
                && msg.TryGetProperty("content", out var content))
            {
                var chunk = content.GetString();
                if (!string.IsNullOrEmpty(chunk)) yield return chunk;
            }
        }
    }
}
