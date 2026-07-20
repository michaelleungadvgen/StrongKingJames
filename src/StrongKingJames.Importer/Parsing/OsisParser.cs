using System.Xml.Linq;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

public class OsisParser
{
    private static readonly XNamespace Osis = "http://www.bibletechnologies.net/2003/OSIS/namespace";

    public IEnumerable<ParsedVerse> Parse(string path)
    {
        var doc = XDocument.Load(path);
        ParsedVerse? current = null;
        var textBuffer = new System.Text.StringBuilder();
        int position = 0;

        foreach (var node in Descendants(doc.Root!))
        {
            if (node is XElement el && el.Name == Osis + "verse")
            {
                var sid = el.Attribute("sID")?.Value;
                var eid = el.Attribute("eID")?.Value;
                if (sid is not null)
                {
                    current = NewVerse(sid);
                    textBuffer.Clear();
                    position = 0;
                }
                else if (eid is not null && current is not null)
                {
                    current.Text = NormalizeWhitespace(textBuffer.ToString());
                    yield return current;
                    current = null;
                }
            }
            else if (current is not null && node is XElement w && w.Name == Osis + "w")
            {
                var lemma = w.Attribute("lemma")?.Value ?? "";
                var numbers = ExtractStrongs(lemma);
                var text = w.Value;
                textBuffer.Append(text).Append(' ');
                position++;
                if (numbers.Count == 0)
                    current.Words.Add(new VerseWord { Position = position, WordText = text, StrongsNumber = null });
                else
                    foreach (var n in numbers)
                        current.Words.Add(new VerseWord { Position = position, WordText = text, StrongsNumber = n });
            }
            else if (current is not null && node is XText t)
            {
                foreach (var token in t.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    textBuffer.Append(token).Append(' ');
                    position++;
                    current.Words.Add(new VerseWord { Position = position, WordText = token, StrongsNumber = null });
                }
            }
        }
    }

    // Yields elements and the text nodes that are NOT inside a <w> element, in document order.
    private static IEnumerable<XNode> Descendants(XElement root)
    {
        foreach (var node in root.Nodes())
        {
            if (node is XElement el)
            {
                yield return el;
                if (el.Name != Osis + "w")
                    foreach (var child in Descendants(el))
                        yield return child;
            }
            else if (node is XText txt && node.Parent?.Name != Osis + "w")
            {
                yield return txt;
            }
        }
    }

    private static ParsedVerse NewVerse(string osisId)
    {
        var parts = osisId.Split('.');
        return new ParsedVerse
        {
            OsisId = osisId,
            BookAbbrev = parts[0],
            Chapter = int.Parse(parts[1]),
            VerseNumber = int.Parse(parts[2]),
        };
    }

    private static List<string> ExtractStrongs(string lemma) =>
        lemma.Split(' ', StringSplitOptions.RemoveEmptyEntries)
             .Where(p => p.StartsWith("strong:"))
             .Select(p => p["strong:".Length..])
             .ToList();

    private static string NormalizeWhitespace(string s) =>
        string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public class ParsedVerse : Verse
{
    public string BookAbbrev { get; set; } = "";
}
