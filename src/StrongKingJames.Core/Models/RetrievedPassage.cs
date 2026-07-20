namespace StrongKingJames.Core.Models;

public record RetrievedPassage(
    string Reference,     // "John 3:16"
    string Text,          // the hit plus expanded neighbors, joined
    double Score);
