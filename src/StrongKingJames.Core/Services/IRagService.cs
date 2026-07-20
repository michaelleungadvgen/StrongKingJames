namespace StrongKingJames.Core.Services;

public interface IRagService
{
    IAsyncEnumerable<string> AnswerAsync(string question, CancellationToken ct = default);
}
