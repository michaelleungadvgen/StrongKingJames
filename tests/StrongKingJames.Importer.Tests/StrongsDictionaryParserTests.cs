using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class StrongsDictionaryParserTests
{
    private static string HebrewFixture =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-strongs-hebrew.xml");

    private static string GreekFixture =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-strongs-greek.xml");

    [Fact]
    public void ParseHebrew_extracts_entry_fields()
    {
        var entry = new StrongsDictionaryParser().ParseHebrew(HebrewFixture).Single();
        Assert.Equal("H430", entry.Number);
        Assert.Equal("אֱלֹהִים", entry.Lemma);          // headword's lemma attr (pointed), not the <w> text
        Assert.Equal("ʼĕlôhîym", entry.Transliteration); // xlit attr
        Assert.Equal("el-o-heem'", entry.Pronunciation); // POS attr holds the pronunciation
        Assert.Contains("supreme God", entry.Definition);
        Assert.DoesNotContain("<hi>", entry.Definition); // child tags stripped, text kept
        Assert.Contains("judges", entry.KjvUsage);
    }

    [Fact]
    public void ParseGreek_extracts_entry_fields()
    {
        // Parsing at all proves the <!DOCTYPE ...> is handled (DtdProcessing.Ignore);
        // XDocument.Load would throw on the DOCTYPE.
        var entry = new StrongsDictionaryParser().ParseGreek(GreekFixture).Single();
        Assert.Equal("G25", entry.Number);
        Assert.Equal("ἀγαπάω", entry.Lemma);            // first direct-child <greek> unicode attr
        Assert.Equal("agapao", entry.Transliteration);   // its translit attr
        Assert.Equal("ag-ap-ah'-o", entry.Pronunciation);
        Assert.Contains("love", entry.Definition);
        Assert.Contains("love", entry.KjvUsage);
        // Leading "--"/":--" is stripped from the KJV usage.
        Assert.False(entry.KjvUsage.StartsWith('-') || entry.KjvUsage.StartsWith(':'));
    }
}
