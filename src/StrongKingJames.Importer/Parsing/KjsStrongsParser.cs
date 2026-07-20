using System.Text.Json;
using System.Text.Json.Serialization;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

// Parses the 1John419/kjs JSON Strong's dictionary (strong_dict.json) into StrongsEntry
// records. This single file covers BOTH Hebrew (H####) and Greek (G####) entries, and its
// numbers match the per-word tags emitted by KjsJsonParser exactly (no padding mismatch).
//
// Entry shape:  { "k": "G2316", "v": [ xlit, translit, lemma, [def...], [meaning...], [usage...] ] }
//
// The kjs data is GPL-3.0; it is a user-fetched runtime input (never bundled/redistributed).
public class KjsStrongsParser
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public IEnumerable<StrongsEntry> Parse(string path)
    {
        List<KjsDictEntry> entries;
        using (var fs = File.OpenRead(path))
            entries = JsonSerializer.Deserialize<List<KjsDictEntry>>(fs, JsonOpts) ?? new();

        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.K)) continue;
            var v = e.V;
            if (v.ValueKind != JsonValueKind.Array) continue;

            var translit = GetString(v, 1);
            var lemma = GetString(v, 2);
            // v[3] = derivation/etymology parts, v[4] = definition parts; combine for a readable Definition.
            var definition = (JoinArray(v, 3) + " " + JoinArray(v, 4)).Trim();
            var kjvUsage = JoinArray(v, 5).Trim();

            yield return new StrongsEntry
            {
                Number = e.K.Trim(),
                Lemma = lemma,
                Transliteration = translit,
                Pronunciation = "",
                Definition = definition,
                KjvUsage = kjvUsage,
            };
        }
    }

    private static string GetString(JsonElement v, int index) =>
        v.GetArrayLength() > index && v[index].ValueKind == JsonValueKind.String
            ? (v[index].GetString() ?? "").Trim()
            : "";

    private static string JoinArray(JsonElement v, int index)
    {
        if (v.GetArrayLength() <= index || v[index].ValueKind != JsonValueKind.Array)
            return "";
        return string.Join(' ', v[index].EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? ""));
    }

    internal sealed class KjsDictEntry
    {
        [JsonPropertyName("k")] public string K { get; set; } = "";
        [JsonPropertyName("v")] public JsonElement V { get; set; }
    }
}
