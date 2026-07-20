using StrongKingJames.Core.Models;
using StrongKingJames.Core.Search;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class SearchModeDetectorTests
{
    [Theory]
    [InlineData("H7225", SearchMode.Strongs)]
    [InlineData("g26", SearchMode.Strongs)]
    [InlineData("G0026", SearchMode.Strongs)]
    [InlineData("John 3:16", SearchMode.Reference)]
    [InlineData("1 John 4", SearchMode.Reference)]
    [InlineData("Gen 1:1", SearchMode.Reference)]
    [InlineData("Genesis 1", SearchMode.Reference)]
    [InlineData("Song of Solomon 2:1", SearchMode.Reference)]
    [InlineData("Song of Songs 1", SearchMode.Reference)]
    [InlineData("what does the bible say about love", SearchMode.Semantic)]
    [InlineData("forgiveness", SearchMode.Semantic)]
    public void Detect_classifies_query(string query, SearchMode expected)
    {
        Assert.Equal(expected, SearchModeDetector.Detect(query));
    }
}
