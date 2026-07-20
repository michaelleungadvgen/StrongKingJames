using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class OsisParserMultiStrongTests
{
    private static string FixturePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-osis-multistrong.xml");

    [Fact]
    public void Parse_word_with_two_strongs_yields_two_words_sharing_position()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        var first = v.Words.Single(w => w.StrongsNumber == "H1234");
        var second = v.Words.Single(w => w.StrongsNumber == "H5678");
        Assert.Equal("grace", first.WordText);
        Assert.Equal("grace", second.WordText);
        Assert.Equal(first.Position, second.Position);
    }

    [Fact]
    public void Parse_mixed_lemma_keeps_only_strong_token()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        var godWords = v.Words.Where(w => w.WordText == "God" && w.StrongsNumber != null).ToList();
        var god = Assert.Single(godWords);
        Assert.Equal("H430", god.StrongsNumber);
        Assert.DoesNotContain(v.Words, w => w.StrongsNumber == "lemma.TH:0247");
        Assert.DoesNotContain(v.Words, w => w.StrongsNumber != null && w.StrongsNumber.Contains("0247"));
    }
}
