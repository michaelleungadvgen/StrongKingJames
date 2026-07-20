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

    // Accepts already-parsed verses so the seeder is format-agnostic (OSIS or kjs JSON).
    public async Task SeedVersesAsync(IEnumerable<ParsedVerse> verses, CancellationToken ct = default)
    {
        // Resume-safe: skip verses already present so a crash mid-seed can be re-run to completion.
        var existing = (await db.Verses.Select(v => v.OsisId).ToListAsync(ct)).ToHashSet();
        var byAbbrev = await db.Books.ToDictionaryAsync(b => b.Abbreviation, ct);
        var batch = new List<Verse>();
        foreach (ParsedVerse pv in verses)
        {
            if (!byAbbrev.TryGetValue(pv.BookAbbrev, out var book)) continue;
            if (existing.Contains(pv.OsisId)) continue;
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
        // Resume-safe: track already-present numbers so a partial run resumes and Hebrew/Greek don't collide.
        var existingNumbers = (await db.StrongsEntries.Select(e => e.Number).ToListAsync(ct)).ToHashSet();
        var parser = new StrongsDictionaryParser();
        foreach (var path in new[] { hebrewPath, greekPath })
        {
            var entries = parser.Parse(path)
                .GroupBy(e => e.Number).Select(g => g.First())  // defensive de-dup within a file
                .Where(e => existingNumbers.Add(e.Number))      // skip numbers already seeded (prior run or earlier file)
                .ToList();
            db.StrongsEntries.AddRange(entries);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }

    // Seeds Strong's entries from the kjs JSON dictionary (single file, both Hebrew + Greek).
    public async Task SeedStrongsFromJsonAsync(string dictPath, CancellationToken ct = default)
    {
        var existingNumbers = (await db.StrongsEntries.Select(e => e.Number).ToListAsync(ct)).ToHashSet();
        var entries = new KjsStrongsParser().Parse(dictPath)
            .GroupBy(e => e.Number).Select(g => g.First())
            .Where(e => existingNumbers.Add(e.Number))
            .ToList();
        db.StrongsEntries.AddRange(entries);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }
}
