using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class VerseWordConfiguration : IEntityTypeConfiguration<VerseWord>
{
    public void Configure(EntityTypeBuilder<VerseWord> b)
    {
        b.ToTable("verse_words");
        b.HasKey(x => x.Id);
        b.Property(x => x.WordText).IsRequired();
        b.HasIndex(x => x.StrongsNumber);
        b.HasIndex(x => new { x.VerseId, x.Position });
    }
}
