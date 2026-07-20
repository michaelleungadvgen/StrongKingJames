namespace StrongKingJames.Core.Models;

public class Verse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int Chapter { get; set; }
    public int VerseNumber { get; set; }
    public string OsisId { get; set; } = ""; // e.g. "Gen.1.1"
    public string Text { get; set; } = "";    // plain concatenated text
    public List<VerseWord> Words { get; set; } = [];
}
