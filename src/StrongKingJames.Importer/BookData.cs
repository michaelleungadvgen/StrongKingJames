using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer;

// Canonical 66-book metadata. Abbreviations match the osisID book codes in the KJV OSIS file.
// Ids are intentionally left unset so EF assigns them on insert.
public static class BookData
{
    public static IReadOnlyList<Book> All { get; } =
    [
        // Old Testament
        Ot(1, "Genesis", "Gen"),
        Ot(2, "Exodus", "Exod"),
        Ot(3, "Leviticus", "Lev"),
        Ot(4, "Numbers", "Num"),
        Ot(5, "Deuteronomy", "Deut"),
        Ot(6, "Joshua", "Josh"),
        Ot(7, "Judges", "Judg"),
        Ot(8, "Ruth", "Ruth"),
        Ot(9, "1 Samuel", "1Sam"),
        Ot(10, "2 Samuel", "2Sam"),
        Ot(11, "1 Kings", "1Kgs"),
        Ot(12, "2 Kings", "2Kgs"),
        Ot(13, "1 Chronicles", "1Chr"),
        Ot(14, "2 Chronicles", "2Chr"),
        Ot(15, "Ezra", "Ezra"),
        Ot(16, "Nehemiah", "Neh"),
        Ot(17, "Esther", "Esth"),
        Ot(18, "Job", "Job"),
        Ot(19, "Psalms", "Ps"),
        Ot(20, "Proverbs", "Prov"),
        Ot(21, "Ecclesiastes", "Eccl"),
        Ot(22, "Song of Solomon", "Song"),
        Ot(23, "Isaiah", "Isa"),
        Ot(24, "Jeremiah", "Jer"),
        Ot(25, "Lamentations", "Lam"),
        Ot(26, "Ezekiel", "Ezek"),
        Ot(27, "Daniel", "Dan"),
        Ot(28, "Hosea", "Hos"),
        Ot(29, "Joel", "Joel"),
        Ot(30, "Amos", "Amos"),
        Ot(31, "Obadiah", "Obad"),
        Ot(32, "Jonah", "Jonah"),
        Ot(33, "Micah", "Mic"),
        Ot(34, "Nahum", "Nah"),
        Ot(35, "Habakkuk", "Hab"),
        Ot(36, "Zephaniah", "Zeph"),
        Ot(37, "Haggai", "Hag"),
        Ot(38, "Zechariah", "Zech"),
        Ot(39, "Malachi", "Mal"),
        // New Testament
        Nt(40, "Matthew", "Matt"),
        Nt(41, "Mark", "Mark"),
        Nt(42, "Luke", "Luke"),
        Nt(43, "John", "John"),
        Nt(44, "Acts", "Acts"),
        Nt(45, "Romans", "Rom"),
        Nt(46, "1 Corinthians", "1Cor"),
        Nt(47, "2 Corinthians", "2Cor"),
        Nt(48, "Galatians", "Gal"),
        Nt(49, "Ephesians", "Eph"),
        Nt(50, "Philippians", "Phil"),
        Nt(51, "Colossians", "Col"),
        Nt(52, "1 Thessalonians", "1Thess"),
        Nt(53, "2 Thessalonians", "2Thess"),
        Nt(54, "1 Timothy", "1Tim"),
        Nt(55, "2 Timothy", "2Tim"),
        Nt(56, "Titus", "Titus"),
        Nt(57, "Philemon", "Phlm"),
        Nt(58, "Hebrews", "Heb"),
        Nt(59, "James", "Jas"),
        Nt(60, "1 Peter", "1Pet"),
        Nt(61, "2 Peter", "2Pet"),
        Nt(62, "1 John", "1John"),
        Nt(63, "2 John", "2John"),
        Nt(64, "3 John", "3John"),
        Nt(65, "Jude", "Jude"),
        Nt(66, "Revelation", "Rev"),
    ];

    private static Book Ot(int sortOrder, string name, string abbrev) =>
        new() { Name = name, Abbreviation = abbrev, Testament = "OT", SortOrder = sortOrder };

    private static Book Nt(int sortOrder, string name, string abbrev) =>
        new() { Name = name, Abbreviation = abbrev, Testament = "NT", SortOrder = sortOrder };
}
