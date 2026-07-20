using System.Xml;
using System.Xml.Linq;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

public class StrongsDictionaryParser
{
    private static readonly XNamespace Osis = "http://www.bibletechnologies.net/2003/OSIS/namespace";

    // Hebrew lexicon: OSIS namespace. Each <div type="entry" n="430"> holds a headword <w>
    // (the FIRST DIRECT child — there are also <w> inside <foreign>/<note>) plus explanation
    // and translation notes. POS on the headword holds the pronunciation in this dataset.
    public IEnumerable<StrongsEntry> ParseHebrew(string path)
    {
        var doc = XDocument.Load(path);
        foreach (var div in doc.Descendants(Osis + "div").Where(d => (string?)d.Attribute("type") == "entry"))
        {
            var n = (string?)div.Attribute("n");
            if (string.IsNullOrWhiteSpace(n)) continue;

            var w = div.Elements(Osis + "w").FirstOrDefault();
            yield return new StrongsEntry
            {
                Number = StrongsNumber.Normalize("H" + n.Trim()),
                Lemma = (string?)w?.Attribute("lemma") ?? "",
                Transliteration = (string?)w?.Attribute("xlit") ?? "",
                Pronunciation = (string?)w?.Attribute("POS") ?? "",
                Definition = NoteText(div, "explanation"),
                KjvUsage = NoteText(div, "translation"),
            };
        }
    }

    // Greek lexicon: NON-OSIS, and prefixed by a <!DOCTYPE ...>. XDocument.Load defaults to
    // DtdProcessing.Prohibit and THROWS on the DOCTYPE, so load via an XmlReader that ignores it.
    public IEnumerable<StrongsEntry> ParseGreek(string path)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(path, settings);
        var doc = XDocument.Load(reader);

        foreach (var entry in doc.Descendants("entry"))
        {
            var strongs = (string?)entry.Attribute("strongs");
            if (string.IsNullOrWhiteSpace(strongs)) continue;

            var greek = entry.Elements("greek").FirstOrDefault(); // first DIRECT child (others nest in derivations)
            yield return new StrongsEntry
            {
                Number = StrongsNumber.Normalize("G" + strongs.Trim()),
                Lemma = (string?)greek?.Attribute("unicode") ?? "",
                Transliteration = (string?)greek?.Attribute("translit") ?? "",
                Pronunciation = (string?)entry.Element("pronunciation")?.Attribute("strongs") ?? "",
                Definition = (entry.Element("strongs_def")?.Value ?? "").Trim(),
                KjvUsage = StripLeadingDashes(entry.Element("kjv_def")?.Value ?? ""),
            };
        }
    }

    // Inner text of the <note type="..."> section (child tags like <hi> stripped, their text kept).
    private static string NoteText(XElement div, string type)
    {
        var note = div.Elements(Osis + "note").FirstOrDefault(n => (string?)n.Attribute("type") == type);
        return note is null ? "" : string.Join(' ', note.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    // KJV defs are prefixed with a lead-in like "--" or ":--"; drop it and any surrounding whitespace.
    private static string StripLeadingDashes(string value) =>
        value.Trim().TrimStart(':', '-', ' ').Trim();
}
