namespace StrongKingJames.Core.Models;

public class Book
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Testament { get; set; } = ""; // "OT" or "NT"
    public int SortOrder { get; set; }
    public List<Verse> Verses { get; set; } = [];
}
