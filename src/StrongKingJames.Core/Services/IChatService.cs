namespace StrongKingJames.Core.Services;

public interface IChatService
{
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<StrongKingJames.Core.Models.ChatMessage> messages,
        CancellationToken ct = default);
}
