using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Data;
using StrongKingJames.Importer.Parsing;

namespace StrongKingJames.Importer.Seeding;

public class DatabaseSeeder(BibleDbContext db)
{
    public async Task SeedBooksAsync(CancellationToken ct = default)
    {
        if (await db.Books.AnyAsync(ct)) return;
        db.Books.AddRange(BookData.All);
        await db.SaveChangesAsync(ct);
    }

    public async Task SeedVersesAsync(string osisPath, CancellationToken ct = default)
    {
        if (await db.Verses.AnyAsync(ct)) return;
        var byAbbrev = await db.Books.ToDictionaryAsync(b => b.Abbreviation, ct);
        var parser = new OsisParser();
        var batch = new List<Verse>();
        foreach (ParsedVerse pv in parser.Parse(osisPath))
        {
            if (!byAbbrev.TryGetValue(pv.BookAbbrev, out var book)) continue;
            // IMPORTANT: map ParsedVerse -> a plain Verse. EF cannot add the unmapped
            // ParsedVerse subclass to DbSet<Verse>; project to Verse (carry Words over).
            var verse = new Verse
            {
                BookId = book.Id,
                Chapter = pv.Chapter,
                VerseNumber = pv.VerseNumber,
                OsisId = pv.OsisId,
                Text = pv.Text,
                Words = pv.Words,
            };
            batch.Add(verse);
            if (batch.Count >= 500)
            {
                db.Verses.AddRange(batch);
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
                batch.Clear();
            }
        }
        if (batch.Count > 0)
        {
            db.Verses.AddRange(batch);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }

    public async Task SeedStrongsAsync(string hebrewPath, string greekPath, CancellationToken ct = default)
    {
        if (await db.StrongsEntries.AnyAsync(ct)) return;
        var parser = new StrongsDictionaryParser();
        foreach (var path in new[] { hebrewPath, greekPath })
        {
            var entries = parser.Parse(path)
                .GroupBy(e => e.Number).Select(g => g.First())  // defensive de-dup within a file
                .ToList();
            db.StrongsEntries.AddRange(entries);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }
}
