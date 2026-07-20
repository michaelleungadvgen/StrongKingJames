using System.Text.RegularExpressions;

namespace StrongKingJames.Core.Rag;

public static partial class CitationExtractor
{
    // Matches "Book c:v" including a leading ordinal (1/2/3) and a two-word book name.
    // NOTE: multi-word names with lowercase connectors (e.g. "Song of Solomon") are only
    // partially matched; broaden the book-name alternation here if full coverage is needed.
    [GeneratedRegex(@"(?:[1-3]\s+)?[A-Z][a-z]+(?:\s+[A-Z][a-z]+)?\s+\d{1,3}:\d{1,3}")]
    private static partial Regex CitationRegex();

    public static IReadOnlyList<string> Extract(string text)
    {
        var seen = new List<string>();
        foreach (Match m in CitationRegex().Matches(text))
        {
            if (!seen.Contains(m.Value)) seen.Add(m.Value);
        }
        return seen;
    }
}
