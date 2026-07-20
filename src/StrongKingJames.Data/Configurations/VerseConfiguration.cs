using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class VerseConfiguration : IEntityTypeConfiguration<Verse>
{
    public void Configure(EntityTypeBuilder<Verse> b)
    {
        b.ToTable("verses");
        b.HasKey(x => x.Id);
        b.Property(x => x.OsisId).IsRequired();
        b.HasIndex(x => x.OsisId).IsUnique();
        b.HasIndex(x => new { x.BookId, x.Chapter, x.VerseNumber });
        b.HasMany(x => x.Words).WithOne().HasForeignKey(w => w.VerseId);
    }
}
