using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface IBibleRepository
{
    Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Verse>> GetChapterAsync(string bookAbbrev, int chapter, CancellationToken ct = default);
    Task<StrongsEntry?> GetStrongsEntryAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResult>> GetVersesByStrongsAsync(string number, CancellationToken ct = default);
    Task<Verse?> GetVerseByReferenceAsync(string bookAbbrev, int chapter, int verse, CancellationToken ct = default);
    Task<IReadOnlyList<Verse>> GetNeighborsAsync(int verseId, int radius, CancellationToken ct = default);

    /// <summary>Literal (case-insensitive) keyword search over verse text, with optional book/testament filters. No embeddings required.</summary>
    Task<IReadOnlyList<SearchResult>> KeywordSearchAsync(
        string query,
        int limit,
        int? bookId = null,
        string? testament = null,
        CancellationToken ct = default);
}
