namespace StrongKingJames.Core.Services;

public interface IChatService
{
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<StrongKingJames.Core.Models.ChatMessage> messages,
        string? model = null,
        CancellationToken ct = default);
}
