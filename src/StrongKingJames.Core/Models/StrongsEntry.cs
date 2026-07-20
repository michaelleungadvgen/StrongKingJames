namespace StrongKingJames.Core.Models;

public class StrongsEntry
{
    public string Number { get; set; } = ""; // PK, e.g. "H7225"
    public string Lemma { get; set; } = "";
    public string Transliteration { get; set; } = "";
    public string Pronunciation { get; set; } = "";
    public string Definition { get; set; } = "";
    public string KjvUsage { get; set; } = "";
}
