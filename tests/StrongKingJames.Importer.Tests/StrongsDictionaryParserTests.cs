using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class StrongsDictionaryParserTests
{
    private static string FixturePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-strongs.xml");

    [Fact]
    public void Parse_extracts_entry_fields()
    {
        var entry = new StrongsDictionaryParser().Parse(FixturePath).Single();
        Assert.Equal("H0430", entry.Number);
        Assert.Equal("אֱלֹהִים", entry.Lemma);
        Assert.Equal("ʼĕlôhîym", entry.Transliteration);
        Assert.Equal("n-m", entry.Pronunciation);
        Assert.Contains("supreme God", entry.Definition);
        Assert.Contains("judges", entry.KjvUsage);
    }
}
