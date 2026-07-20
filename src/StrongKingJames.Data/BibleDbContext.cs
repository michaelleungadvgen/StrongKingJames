using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Data;

public class BibleDbContext(DbContextOptions<BibleDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Verse> Verses => Set<Verse>();
    public DbSet<VerseWord> VerseWords => Set<VerseWord>();
    public DbSet<StrongsEntry> StrongsEntries => Set<StrongsEntry>();
    public DbSet<VerseEmbedding> VerseEmbeddings => Set<VerseEmbedding>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasPostgresExtension("vector");
        mb.ApplyConfigurationsFromAssembly(typeof(BibleDbContext).Assembly);
    }
}
