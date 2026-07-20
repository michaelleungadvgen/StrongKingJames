using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.ToTable("books");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.Property(x => x.Abbreviation).IsRequired();
        b.HasIndex(x => x.Abbreviation).IsUnique();
        b.HasMany(x => x.Verses).WithOne(v => v.Book!).HasForeignKey(v => v.BookId);
    }
}
