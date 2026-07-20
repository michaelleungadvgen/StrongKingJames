using StrongKingJames.Core.Rag;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class CitationExtractorTests
{
    [Fact]
    public void Extract_finds_references_in_order_without_duplicates()
    {
        var text = "Love is central (John 3:16). See also 1 John 4:8 and again John 3:16.";
        var refs = CitationExtractor.Extract(text);
        Assert.Equal(new[] { "John 3:16", "1 John 4:8" }, refs);
    }

    [Fact]
    public void Extract_returns_empty_when_none()
    {
        Assert.Empty(CitationExtractor.Extract("No citations here."));
    }
}
