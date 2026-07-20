using System.Text.Json;
using System.Text.Json.Serialization;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

// Parses the 1John419/kjs JSON Bible format so the importer can seed the KJV with
// per-word Strong's numbers for BOTH testaments (OT Hebrew H#### and NT Greek G####).
//
// Two source files are required (both from https://github.com/1John419/kjs/json):
//   - kjv_pure.json:  { "verses": [ { "k": <verseIndex>, "v": [text, bookIdx0, globalChap, "Book c:v", verseInChap] }, ... ] }
//   - strong_pure.json: { "maps": [ { "k": <verseIndex>, "v": [ [wordText, [strongs...]], ... ] }, ... ] }
// The two files share the same verse index "k", so per-word Strong's tags are joined
// onto the verse text by k. Words with an empty strongs array are untagged (StrongsNumber = null).
// bookIdx0 is the 0-based book index and lines up with BookData.All (Matthew = 39).
//
// The kjs data is GPL-3.0; it is a user-fetched runtime input (never bundled/redistributed).
public class KjsJsonParser
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public IEnumerable<ParsedVerse> Parse(string kjvPath, string strongsPath)
    {
        var verses = LoadKeyed(kjvPath, "verses");
        var words = LoadKeyed(strongsPath, "maps");

        foreach (var (k, vv) in verses.OrderBy(kv => kv.Key))
        {
            var v = vv.V;
            if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() < 5) continue;

            var text = v[0].GetString() ?? "";
            var bookIdx = v[1].ValueKind == JsonValueKind.Number ? v[1].GetInt32() : -1;
            var refStr = v[3].GetString() ?? "";

            if (bookIdx < 0 || bookIdx >= BookData.All.Count) continue;
            var abbrev = BookData.All[bookIdx].Abbreviation;

            if (!TryParseReference(refStr, out var chapter, out var verseNumber)) continue;

            var pv = new ParsedVerse
            {
                OsisId = $"{abbrev}.{chapter}.{verseNumber}",
                BookAbbrev = abbrev,
                Chapter = chapter,
                VerseNumber = verseNumber,
                Text = NormalizeWhitespace(text),
            };

            int position = 0;
            if (words.TryGetValue(k, out var we) && we.V.ValueKind == JsonValueKind.Array)
            {
                foreach (var wordEl in we.V.EnumerateArray())
                {
                    if (wordEl.ValueKind != JsonValueKind.Array || wordEl.GetArrayLength() < 2) continue;
                    var wordText = wordEl[0].GetString() ?? "";
                    var strongs = wordEl[1].ValueKind == JsonValueKind.Array
                        ? wordEl[1].EnumerateArray()
                            .Select(e => e.GetString())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s!.Trim())
                            .ToList()
                        : new List<string>();

                    position++;
                    if (strongs.Count == 0)
                        pv.Words.Add(new VerseWord { Position = position, WordText = wordText, StrongsNumber = null });
                    else
                        foreach (var s in strongs)
                            pv.Words.Add(new VerseWord { Position = position, WordText = wordText, StrongsNumber = s });
                }
            }
            else
            {
                // No per-word Strong's entry for this verse: emit the plain text as untagged tokens.
                foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    position++;
                    pv.Words.Add(new VerseWord { Position = position, WordText = token, StrongsNumber = null });
                }
            }

            yield return pv;
        }
    }

    private static Dictionary<int, KjsVerseEntry> LoadKeyed(string path, string propertyName)
    {
        using var fs = File.OpenRead(path);
        var doc = JsonSerializer.Deserialize<KjsFile>(fs, JsonOpts)
                  ?? throw new InvalidDataException($"Could not parse JSON file: {path}");
        var list = propertyName == "verses" ? doc.Verses : doc.Maps;
        return list.ToDictionary(e => e.K);
    }

    private static bool TryParseReference(string refStr, out int chapter, out int verseNumber)
    {
        chapter = 0; verseNumber = 0;
        var lastSpace = refStr.LastIndexOf(' ');
        if (lastSpace < 0) return false;
        var cv = refStr[(lastSpace + 1)..];
        var colon = cv.IndexOf(':');
        if (colon < 0) return false;
        if (!int.TryParse(cv[..colon], out chapter)) return false;
        return int.TryParse(cv[(colon + 1)..], out verseNumber);
    }

    private static string NormalizeWhitespace(string s) =>
        string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    internal sealed class KjsFile
    {
        [JsonPropertyName("verses")] public List<KjsVerseEntry> Verses { get; set; } = new();
        [JsonPropertyName("maps")] public List<KjsVerseEntry> Maps { get; set; } = new();
    }

    internal sealed class KjsVerseEntry
    {
        [JsonPropertyName("k")] public int K { get; set; }
        [JsonPropertyName("v")] public JsonElement V { get; set; }
    }
}
