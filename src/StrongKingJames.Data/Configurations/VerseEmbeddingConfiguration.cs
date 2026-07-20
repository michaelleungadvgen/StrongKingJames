using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Data.Configurations;

public class VerseEmbeddingConfiguration : IEntityTypeConfiguration<VerseEmbedding>
{
    public void Configure(EntityTypeBuilder<VerseEmbedding> b)
    {
        b.ToTable("verse_embeddings");
        b.HasKey(x => x.VerseId);
        b.Property(x => x.Embedding).HasColumnType("vector(768)");
    }
}
