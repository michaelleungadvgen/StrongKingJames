namespace StrongKingJames.Core.Models;

public class Note
{
    public int Id { get; set; }
    public NoteType Type { get; set; } = NoteType.General;

    /// <summary>
    /// Optional target the note is about: a book abbreviation (e.g. "John"), a verse
    /// reference/osisId (e.g. "John.3.16"), or a character name (e.g. "Moses"). Null for General.
    /// </summary>
    public string? Reference { get; set; }

    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Combined text used for embedding and display context.</summary>
    public string ToEmbeddingText()
    {
        var target = string.IsNullOrWhiteSpace(Reference) ? Type.ToString() : $"{Type} {Reference}";
        return $"Note about {target}: {Title}\n{Body}".Trim();
    }
}
