using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class OsisParserNewTestamentTests
{
    private static string FixturePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-osis-nt.xml");

    [Fact]
    public void Parse_extracts_new_testament_verses_with_references()
    {
        var verses = new OsisParser().Parse(FixturePath).ToList();
        Assert.Equal(2, verses.Count);
        Assert.Equal("John.3.16", verses[0].OsisId);
        Assert.Equal("John", verses[0].BookAbbrev);
        Assert.Equal(3, verses[0].Chapter);
        Assert.Equal(16, verses[0].VerseNumber);
        Assert.Equal("John.3.17", verses[1].OsisId);
    }

    [Fact]
    public void Parse_captures_greek_strongs_tagged_words()
    {
        var verses = new OsisParser().Parse(FixturePath).ToList();
        var v16 = verses.Single(v => v.OsisId == "John.3.16");
        // Greek Strong's numbers (G####) are preserved exactly, including the G prefix.
        Assert.Contains(v16.Words, w => w.StrongsNumber == "G2316" && w.WordText.Contains("God"));
        Assert.Contains(v16.Words, w => w.StrongsNumber == "G25" && w.WordText == "loved");
        Assert.Contains(v16.Words, w => w.StrongsNumber == "G3779" && w.WordText == "so");
        // The NT verse also carries untagged tokens with null Strong's.
        Assert.Contains(v16.Words, w => w.StrongsNumber == null);
    }

    [Fact]
    public void Parse_assembles_plain_text_from_tagged_words()
    {
        var v16 = new OsisParser().Parse(FixturePath).First(v => v.OsisId == "John.3.16");
        Assert.Contains("God", v16.Text);
        Assert.Contains("loved", v16.Text);
        Assert.Contains("world", v16.Text);
    }

    [Fact]
    public void Parse_distinct_old_and_new_testament_strongs_do_not_collide()
    {
        // The parser must not conflate H#### and G#### namespaces: a Greek G2316 and a
        // (hypothetical) Hebrew H2316 are different Strong's entries. Verifying the parser
        // preserves the leading letter proves the New Testament path is wired in correctly.
        var ntWords = new OsisParser().Parse(FixturePath)
            .SelectMany(v => v.Words)
            .Where(w => w.StrongsNumber is { Length: > 1 } s && (s[0] == 'G' || s[0] == 'H'))
            .ToList();
        Assert.All(ntWords, w => Assert.StartsWith("G", w.StrongsNumber));
        Assert.NotEmpty(ntWords);
    }
}
