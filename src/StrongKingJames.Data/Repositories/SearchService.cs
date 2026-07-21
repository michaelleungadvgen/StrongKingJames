using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Data.Repositories;

public class SearchService(BibleDbContext db) : ISearchService
{
    public async Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(
        float[] queryEmbedding, int topK, int? bookId = null, string? testament = null,
        CancellationToken ct = default)
    {
        var q = new Vector(queryEmbedding);
        // Join to verses/books first so any book/testament filter is applied BEFORE Take(topK)
        // (otherwise we'd take the k nearest and then filter, yielding fewer than k).
        return await db.VerseEmbeddings
            .Join(db.Verses, e => e.VerseId, v => v.Id, (e, v) => new { e, v })
            .Where(x => bookId == null || x.v.BookId == bookId)
            .Where(x => testament == null || x.v.Book!.Testament == testament)
            .Select(x => new { x.v, Distance = x.e.Embedding.CosineDistance(q) })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Select(x => new SearchResult(
                x.v.Id, x.v.OsisId,
                x.v.Book!.Name + " " + x.v.Chapter + ":" + x.v.VerseNumber,
                x.v.Text,
                1.0 - x.Distance))
            .ToListAsync(ct);
    }
}
