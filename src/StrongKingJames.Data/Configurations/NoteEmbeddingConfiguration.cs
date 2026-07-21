using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Data.Configurations;

public class NoteEmbeddingConfiguration : IEntityTypeConfiguration<NoteEmbedding>
{
    public void Configure(EntityTypeBuilder<NoteEmbedding> b)
    {
        b.ToTable("note_embeddings");
        b.HasKey(x => x.NoteId);
        b.Property(x => x.Embedding).HasColumnType("vector(768)");
        // Deleting a note removes its embedding.
        b.HasOne<Note>().WithOne().HasForeignKey<NoteEmbedding>(x => x.NoteId).OnDelete(DeleteBehavior.Cascade);
        // HNSW cosine index added via raw SQL in the migration.
    }
}
