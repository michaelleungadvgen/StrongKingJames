using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class KjsStrongsParserTests
{
    private static string DictPath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-kjs-dict.json");

    [Fact]
    public void Parse_reads_hebrew_and_greek_entries()
    {
        var entries = new KjsStrongsParser().Parse(DictPath).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Number == "H430");
        Assert.Contains(entries, e => e.Number == "G2316");
    }

    [Fact]
    public void Parse_maps_dictionary_fields()
    {
        var g2316 = new KjsStrongsParser().Parse(DictPath).Single(e => e.Number == "G2316");
        Assert.Equal("theos", g2316.Transliteration);
        Assert.Equal("theh'-os", g2316.Lemma);
        Assert.Contains("supreme Divinity", g2316.Definition);
        Assert.Contains("God", g2316.KjvUsage);
    }

    [Fact]
    public void Parse_hebrew_entry_fields()
    {
        var h430 = new KjsStrongsParser().Parse(DictPath).Single(e => e.Number == "H430");
        Assert.Equal("el-o-heem'", h430.Lemma);
        Assert.Contains("supreme God", h430.Definition);
        Assert.Contains("gods", h430.KjvUsage);
    }
}
