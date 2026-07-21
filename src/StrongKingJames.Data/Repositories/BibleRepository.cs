using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Data.Repositories;

public class BibleRepository(BibleDbContext db) : IBibleRepository
{
    public async Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken ct = default) =>
        await db.Books.OrderBy(b => b.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<Verse>> GetChapterAsync(string bookAbbrev, int chapter, CancellationToken ct = default) =>
        await db.Verses
            .Include(v => v.Words.OrderBy(w => w.Position))
            .Where(v => v.Book!.Abbreviation == bookAbbrev && v.Chapter == chapter)
            .OrderBy(v => v.VerseNumber)
            .ToListAsync(ct);

    public async Task<StrongsEntry?> GetStrongsEntryAsync(string number, CancellationToken ct = default) =>
        await db.StrongsEntries.FirstOrDefaultAsync(e => e.Number == number, ct);

    public async Task<IReadOnlyList<SearchResult>> GetVersesByStrongsAsync(string number, CancellationToken ct = default) =>
        await db.VerseWords
            .Where(w => w.StrongsNumber == number)
            .Select(w => w.VerseId).Distinct()
            .Join(db.Verses, id => id, v => v.Id, (id, v) => v)
            .OrderBy(v => v.Book!.SortOrder).ThenBy(v => v.Chapter).ThenBy(v => v.VerseNumber)
            .Select(v => new SearchResult(v.Id, v.OsisId, v.Book!.Name + " " + v.Chapter + ":" + v.VerseNumber, v.Text, null))
            .ToListAsync(ct);

    public async Task<Verse?> GetVerseByReferenceAsync(string bookAbbrev, int chapter, int verse, CancellationToken ct = default) =>
        await db.Verses.FirstOrDefaultAsync(v =>
            v.Book!.Abbreviation == bookAbbrev && v.Chapter == chapter && v.VerseNumber == verse, ct);

    public async Task<IReadOnlyList<Verse>> GetNeighborsAsync(int verseId, int radius, CancellationToken ct = default)
    {
        var v = await db.Verses.FirstOrDefaultAsync(x => x.Id == verseId, ct);
        if (v is null) return [];
        return await db.Verses
            .Where(x => x.BookId == v.BookId && x.Chapter == v.Chapter
                        && x.VerseNumber >= v.VerseNumber - radius
                        && x.VerseNumber <= v.VerseNumber + radius)
            .OrderBy(x => x.VerseNumber)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SearchResult>> KeywordSearchAsync(
        string query, int limit, int? bookId = null, string? testament = null,
        CancellationToken ct = default)
    {
        // Escape LIKE wildcards in the user's text, then match case-insensitively anywhere in the verse.
        var escaped = query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        var pattern = $"%{escaped}%";
        return await db.Verses
            .Where(v => EF.Functions.ILike(v.Text, pattern, "\\"))
            .Where(v => bookId == null || v.BookId == bookId)
            .Where(v => testament == null || v.Book!.Testament == testament)
            .OrderBy(v => v.Book!.SortOrder).ThenBy(v => v.Chapter).ThenBy(v => v.VerseNumber)
            .Take(limit)
            .Select(v => new SearchResult(
                v.Id, v.OsisId,
                v.Book!.Name + " " + v.Chapter + ":" + v.VerseNumber,
                v.Text,
                null))
            .ToListAsync(ct);
    }
}
