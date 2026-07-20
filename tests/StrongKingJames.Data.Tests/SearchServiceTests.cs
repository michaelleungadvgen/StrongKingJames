using Microsoft.EntityFrameworkCore;
using Pgvector;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Entities;
using StrongKingJames.Data.Repositories;
using Xunit;

namespace StrongKingJames.Data.Tests;

[Collection("db")]
public class SearchServiceTests(DatabaseFixture fx)
{
    private static float[] UnitVec(int dim, int hot)
    {
        var a = new float[dim];
        a[hot] = 1f;
        return a;
    }

    [Fact]
    public async Task SemanticSearch_ranks_closest_first()
    {
        await using (var ctx = fx.CreateContext())
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "TRUNCATE verse_words, verse_embeddings, verses, strongs_entries, books RESTART IDENTITY CASCADE;");
            var b = new Book { Name = "Gen", Abbreviation = "Gen", Testament = "OT", SortOrder = 1 };
            ctx.Books.Add(b);
            await ctx.SaveChangesAsync();
            var v1 = new Verse { BookId = b.Id, Chapter = 1, VerseNumber = 1, OsisId = "Gen.1.1", Text = "one" };
            var v2 = new Verse { BookId = b.Id, Chapter = 1, VerseNumber = 2, OsisId = "Gen.1.2", Text = "two" };
            ctx.Verses.AddRange(v1, v2);
            await ctx.SaveChangesAsync();
            ctx.VerseEmbeddings.Add(new VerseEmbedding { VerseId = v1.Id, Embedding = new Vector(UnitVec(768, 0)) });
            ctx.VerseEmbeddings.Add(new VerseEmbedding { VerseId = v2.Id, Embedding = new Vector(UnitVec(768, 5)) });
            await ctx.SaveChangesAsync();
        }

        var svc = new SearchService(fx.CreateContext());
        var results = await svc.SemanticSearchAsync(UnitVec(768, 0), topK: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("Gen.1.1", results[0].OsisId);
        Assert.True(results[0].Score > 0.99);
    }
}
