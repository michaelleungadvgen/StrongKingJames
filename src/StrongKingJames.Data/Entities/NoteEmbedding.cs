using Pgvector;

namespace StrongKingJames.Data.Entities;

public class NoteEmbedding
{
    public int NoteId { get; set; }
    public Vector Embedding { get; set; } = null!;
}
