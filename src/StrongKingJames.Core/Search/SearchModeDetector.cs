using System.Text.RegularExpressions;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Search;

public static partial class SearchModeDetector
{
    [GeneratedRegex(@"^[HGhg]\d{1,5}$")]
    private static partial Regex StrongsRegex();

    [GeneratedRegex(@"^(?:[1-3]\s+)?[A-Za-z]+(?:\s+[A-Za-z]+)?\s+\d{1,3}(?::\d{1,3})?$")]
    private static partial Regex ReferenceRegex();

    public static SearchMode Detect(string query)
    {
        var q = query.Trim();
        if (StrongsRegex().IsMatch(q)) return SearchMode.Strongs;
        if (ReferenceRegex().IsMatch(q)) return SearchMode.Reference;
        return SearchMode.Semantic;
    }
}
