namespace StrongKingJames.Core.Models;

public record NoteSearchResult(
    int NoteId,
    NoteType Type,
    string? Reference,
    string Title,
    string Body,
    double Score);
