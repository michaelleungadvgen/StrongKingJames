using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class StrongsEntryConfiguration : IEntityTypeConfiguration<StrongsEntry>
{
    public void Configure(EntityTypeBuilder<StrongsEntry> b)
    {
        b.ToTable("strongs_entries");
        b.HasKey(x => x.Number);
        b.Property(x => x.Number).HasMaxLength(8);
    }
}
