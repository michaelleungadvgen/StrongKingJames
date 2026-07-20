using StrongKingJames.Core.Models;
using StrongKingJames.Core.Rag;
using StrongKingJames.Core.Services;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class RagServiceTests
{
    private sealed class FakeEmbedder : IEmbeddingService
    {
        public Task<float[]> EmbedAsync(string t, CancellationToken ct = default) => Task.FromResult(new float[768]);
    }
    private sealed class FakeSearch : ISearchService
    {
        public Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(float[] e, int k, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SearchResult>>(new[]
            { new SearchResult(1, "John.3.16", "John 3:16", "For God so loved the world", 0.9) });
    }
    private sealed class FakeRepo : IBibleRepository
    {
        public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task<IReadOnlyList<Verse>> GetChapterAsync(string b, int c, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Verse>>([]);
        public Task<StrongsEntry?> GetStrongsEntryAsync(string n, CancellationToken ct = default) => Task.FromResult<StrongsEntry?>(null);
        public Task<IReadOnlyList<SearchResult>> GetVersesByStrongsAsync(string n, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SearchResult>>([]);
        public Task<Verse?> GetVerseByReferenceAsync(string b, int c, int v, CancellationToken ct = default) => Task.FromResult<Verse?>(null);
        public Task<IReadOnlyList<Verse>> GetNeighborsAsync(int id, int r, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Verse>>(new[] { new Verse { Id = 1, Chapter = 3, VerseNumber = 16, Text = "For God so loved the world" } });
    }
    private sealed class FakeChat : IChatService
    {
        public async IAsyncEnumerable<string> StreamAsync(IReadOnlyList<ChatMessage> m,
            string? model = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            Assert.Contains(m, x => x.Content.Contains("For God so loved the world"));
            yield return "God ";
            yield return "is love (John 3:16).";
        }
    }

    [Fact]
    public async Task AnswerAsync_streams_answer_from_retrieved_passages()
    {
        var svc = new RagService(new FakeEmbedder(), new FakeSearch(), new FakeRepo(), new FakeChat());
        var chunks = new List<string>();
        await foreach (var c in svc.AnswerAsync("What is love?"))
            chunks.Add(c);
        var full = string.Concat(chunks);
        Assert.Contains("John 3:16", full);
    }
}
