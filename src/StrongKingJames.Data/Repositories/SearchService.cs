using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Data.Repositories;

public class SearchService(BibleDbContext db) : ISearchService
{
    public async Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(
        float[] queryEmbedding, int topK, CancellationToken ct = default)
    {
        var q = new Vector(queryEmbedding);
        return await db.VerseEmbeddings
            .OrderBy(e => e.Embedding.CosineDistance(q))
            .Take(topK)
            .Join(db.Verses, e => e.VerseId, v => v.Id, (e, v) => new { e, v })
            .Select(x => new SearchResult(
                x.v.Id, x.v.OsisId,
                x.v.Book!.Name + " " + x.v.Chapter + ":" + x.v.VerseNumber,
                x.v.Text,
                1.0 - x.e.Embedding.CosineDistance(q)))
            .ToListAsync(ct);
    }
}
