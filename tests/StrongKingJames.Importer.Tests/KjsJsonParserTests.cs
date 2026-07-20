using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class KjsJsonParserTests
{
    private static string BiblePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-kjs-bible.json");
    private static string StrongsPath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-kjs-strongs.json");

    private static System.Collections.Generic.IReadOnlyList<ParsedVerse> Parse() =>
        new KjsJsonParser().Parse(BiblePath, StrongsPath).ToList();

    [Fact]
    public void Parse_reads_both_testaments_with_references()
    {
        var verses = Parse();
        Assert.Equal(2, verses.Count);
        var gen = verses.Single(v => v.OsisId == "Gen.1.1");
        Assert.Equal("Gen", gen.BookAbbrev);
        Assert.Equal(1, gen.Chapter);
        Assert.Equal(1, gen.VerseNumber);

        var john = verses.Single(v => v.OsisId == "John.3.16");
        Assert.Equal("John", john.BookAbbrev);
        Assert.Equal(3, john.Chapter);
        Assert.Equal(16, john.VerseNumber);
    }

    [Fact]
    public void Parse_captures_old_testament_hebrew_strongs()
    {
        var gen = Parse().Single(v => v.OsisId == "Gen.1.1");
        Assert.Contains(gen.Words, w => w.StrongsNumber == "H430" && w.WordText == "God");
        Assert.Contains(gen.Words, w => w.StrongsNumber == "H7225");
    }

    [Fact]
    public void Parse_captures_new_testament_greek_strongs()
    {
        var john = Parse().Single(v => v.OsisId == "John.3.16");
        // Single Greek Strong's on a word.
        Assert.Contains(john.Words, w => w.StrongsNumber == "G2316" && w.WordText == "God");
        Assert.Contains(john.Words, w => w.StrongsNumber == "G2889");
        // Multiple Strong's on one word share the same Position (matches OSIS parser behavior).
        var soLoved = john.Words.Where(w => w.WordText == "so loved").ToList();
        Assert.Equal(2, soLoved.Count);
        Assert.Equal(soLoved[0].Position, soLoved[1].Position);
        Assert.Contains(soLoved, w => w.StrongsNumber == "G3779");
        Assert.Contains(soLoved, w => w.StrongsNumber == "G25");
    }

    [Fact]
    public void Parse_keeps_untagged_words_with_null_strongs()
    {
        var john = Parse().Single(v => v.OsisId == "John.3.16");
        // "For" was given an empty strongs array -> untagged.
        Assert.Contains(john.Words, w => w.WordText == "For" && w.StrongsNumber == null);
    }

    [Fact]
    public void Parse_assembles_plain_text_from_verse_record()
    {
        var john = Parse().Single(v => v.OsisId == "John.3.16");
        Assert.Contains("God", john.Text);
        Assert.Contains("world", john.Text);
        Assert.Contains("only begotten", john.Text);
    }

    [Fact]
    public void Parse_assigns_sequential_distinct_positions_per_word_group()
    {
        var john = Parse().Single(v => v.OsisId == "John.3.16");
        var positions = john.Words.Select(w => w.Position).ToList();
        Assert.Equal(positions.OrderBy(p => p).ToList(), positions);
        // Positions are distinct across word groups (multi-strong words share a position,
        // so distinctness is across groups, not across every VerseWord row).
        var groupPositions = john.Words.GroupBy(w => w.Position).Select(g => g.Key).ToList();
        Assert.Equal(groupPositions.OrderBy(p => p).ToList(), groupPositions);
        Assert.Equal(groupPositions.Distinct().Count(), groupPositions.Count);
    }
}
