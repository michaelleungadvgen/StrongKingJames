using System.Runtime.CompilerServices;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Rag;

public class RagService(
    IEmbeddingService embedder,
    ISearchService search,
    IBibleRepository repo,
    IChatService chat) : IRagService
{
    private const int TopK = 8;
    private const int NeighborRadius = 2;

    public async IAsyncEnumerable<string> AnswerAsync(
        string question, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var qvec = await embedder.EmbedAsync(question, ct);
        var hits = await search.SemanticSearchAsync(qvec, TopK, ct);

        var passages = new List<RetrievedPassage>();
        foreach (var hit in hits)
        {
            var neighbors = await repo.GetNeighborsAsync(hit.VerseId, NeighborRadius, ct);
            var text = neighbors.Count > 0
                ? string.Join(' ', neighbors.Select(n => n.Text))
                : hit.Text;
            passages.Add(new RetrievedPassage(hit.Reference, text, hit.Score ?? 0));
        }

        var messages = RagPromptBuilder.Build(question, passages);
        await foreach (var chunk in chat.StreamAsync(messages, ct))
            yield return chunk;
    }
}
