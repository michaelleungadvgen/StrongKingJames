namespace StrongKingJames.Core.Models;

public class VerseWord
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public int Position { get; set; }
    public string WordText { get; set; } = "";
    public string? StrongsNumber { get; set; } // null for untagged tokens/punctuation
}
