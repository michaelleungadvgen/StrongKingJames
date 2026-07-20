namespace StrongKingJames.Importer.Parsing;

// Shared normalization for Strong's numbers so verse-word tags and dictionary entries JOIN.
// Verse words come from OSIS lemmas ("strong:H07225") and dictionaries derive numbers from
// bare/zero-padded ids ("430", "00025"). All are reduced to <Letter><integer-without-leading-zeros>:
//   "strong:H07225" -> "H7225", "H0430" -> "H430", "G0025" -> "G25", "H430" -> "H430".
public static class StrongsNumber
{
    public static string Normalize(string letterOrLemma)
    {
        if (string.IsNullOrWhiteSpace(letterOrLemma)) return "";
        var s = letterOrLemma.Trim();
        if (s.StartsWith("strong:", StringComparison.OrdinalIgnoreCase))
            s = s["strong:".Length..].Trim();
        if (s.Length < 2) return s.ToUpperInvariant();

        var letter = char.ToUpperInvariant(s[0]);
        var digits = new string(s.Skip(1).TakeWhile(char.IsDigit).ToArray());
        if (digits.Length == 0) return s.ToUpperInvariant();
        return $"{letter}{int.Parse(digits)}";
    }
}
