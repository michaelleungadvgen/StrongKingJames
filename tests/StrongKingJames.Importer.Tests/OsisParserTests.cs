using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class OsisParserTests
{
    private static string FixturePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-osis.xml");

    [Fact]
    public void Parse_extracts_verse_with_reference_and_text()
    {
        var verses = new OsisParser().Parse(FixturePath).ToList();
        var v = Assert.Single(verses);
        Assert.Equal("Gen.1.1", v.OsisId);
        Assert.Equal(1, v.Chapter);
        Assert.Equal(1, v.VerseNumber);
        Assert.Contains("beginning", v.Text);
        Assert.Contains("God", v.Text);
    }

    [Fact]
    public void Parse_captures_strongs_tagged_words()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        Assert.Contains(v.Words, w => w.StrongsNumber == "H0430" && w.WordText.Contains("God"));
        Assert.Contains(v.Words, w => w.StrongsNumber == "H07225");
    }

    [Fact]
    public void Parse_keeps_untagged_tokens_with_null_strongs()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        Assert.Contains(v.Words, w => w.StrongsNumber == null);
    }

    [Fact]
    public void Parse_assigns_sequential_positions()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        var positions = v.Words.Select(w => w.Position).ToList();
        Assert.Equal(positions.OrderBy(p => p).ToList(), positions);
        Assert.Equal(positions.Distinct().Count(), positions.Count);
    }
}
