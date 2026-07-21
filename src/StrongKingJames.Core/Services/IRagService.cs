using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface IRagService
{
    /// <summary>
    /// Answers a question grounded in retrieved scripture. Optional <paramref name="history"/>
    /// carries prior conversation turns (user/assistant) so the chat is contextual.
    /// </summary>
    IAsyncEnumerable<string> AnswerAsync(
        string question,
        string? model = null,
        IReadOnlyList<ChatMessage>? history = null,
        bool includeNotes = false,
        CancellationToken ct = default);
}
