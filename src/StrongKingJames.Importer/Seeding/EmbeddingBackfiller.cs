using Microsoft.EntityFrameworkCore;
using Pgvector;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Importer.Seeding;

public class EmbeddingBackfiller(BibleDbContext db, IEmbeddingService embedder)
{
    public async Task<int> RunAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var pending = await db.Verses
            .Where(v => !db.VerseEmbeddings.Any(e => e.VerseId == v.Id))
            .Select(v => new { v.Id, v.Text })
            .ToListAsync(ct);

        int done = 0;
        foreach (var v in pending)
        {
            var vec = await embedder.EmbedAsync(v.Text, ct);
            db.VerseEmbeddings.Add(new VerseEmbedding { VerseId = v.Id, Embedding = new Vector(vec) });
            await db.SaveChangesAsync(ct);
            progress?.Report(++done);
        }
        return done;
    }
}
