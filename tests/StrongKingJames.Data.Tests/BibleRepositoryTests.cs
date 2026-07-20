using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Repositories;
using Xunit;

namespace StrongKingJames.Data.Tests;

[Collection("db")]
public class BibleRepositoryTests(DatabaseFixture fx)
{
    private async Task SeedAsync()
    {
        await using var ctx = fx.CreateContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE verse_words, verse_embeddings, verses, strongs_entries, books RESTART IDENTITY CASCADE;");

        var john = new Book { Name = "John", Abbreviation = "John", Testament = "NT", SortOrder = 43 };
        ctx.Books.Add(john);
        await ctx.SaveChangesAsync();

        var v16 = new Verse { BookId = john.Id, Chapter = 3, VerseNumber = 16, OsisId = "John.3.16", Text = "For God so loved the world" };
        var v17 = new Verse { BookId = john.Id, Chapter = 3, VerseNumber = 17, OsisId = "John.3.17", Text = "For God sent not his Son to condemn" };
        ctx.Verses.AddRange(v16, v17);
        await ctx.SaveChangesAsync();

        ctx.VerseWords.Add(new VerseWord { VerseId = v16.Id, Position = 1, WordText = "God", StrongsNumber = "G2316" });
        ctx.VerseWords.Add(new VerseWord { VerseId = v17.Id, Position = 1, WordText = "God", StrongsNumber = "G2316" });
        ctx.StrongsEntries.Add(new StrongsEntry { Number = "G2316", Lemma = "θεός", Transliteration = "theos", Pronunciation = "theh'-os", Definition = "a deity", KjvUsage = "God" });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetChapter_returns_ordered_verses_with_words()
    {
        await SeedAsync();
        var repo = new BibleRepository(fx.CreateContext());
        var verses = await repo.GetChapterAsync("John", 3);
        Assert.Equal(2, verses.Count);
        Assert.Equal(16, verses[0].VerseNumber);
        Assert.Contains(verses[0].Words, w => w.StrongsNumber == "G2316");
    }

    [Fact]
    public async Task GetStrongsEntry_returns_entry()
    {
        await SeedAsync();
        var repo = new BibleRepository(fx.CreateContext());
        var entry = await repo.GetStrongsEntryAsync("G2316");
        Assert.NotNull(entry);
        Assert.Equal("theos", entry!.Transliteration);
    }

    [Fact]
    public async Task GetVersesByStrongs_returns_all_matches()
    {
        await SeedAsync();
        var repo = new BibleRepository(fx.CreateContext());
        var results = await repo.GetVersesByStrongsAsync("G2316");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.StartsWith("John 3:", r.Reference));
    }

    [Fact]
    public async Task GetNeighbors_returns_window_within_chapter()
    {
        await SeedAsync();
        await using var ctx = fx.CreateContext();
        var v16 = await ctx.Verses.FirstAsync(v => v.OsisId == "John.3.16");
        var repo = new BibleRepository(fx.CreateContext());
        var neighbors = await repo.GetNeighborsAsync(v16.Id, radius: 2);
        Assert.Contains(neighbors, v => v.VerseNumber == 16);
        Assert.Contains(neighbors, v => v.VerseNumber == 17);
    }
}
