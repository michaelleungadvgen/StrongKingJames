using Pgvector;

namespace StrongKingJames.Data.Entities;

public class VerseEmbedding
{
    public int VerseId { get; set; }
    public Vector Embedding { get; set; } = null!;
}
