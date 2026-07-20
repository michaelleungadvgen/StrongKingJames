namespace StrongKingJames.Core.Models;

public record SearchResult(
    int VerseId,
    string OsisId,
    string Reference, // e.g. "John 3:16"
    string Text,
    double? Score);   // similarity score for semantic; null otherwise
