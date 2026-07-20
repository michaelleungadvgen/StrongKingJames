using System.Xml.Linq;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

public class StrongsDictionaryParser
{
    private static readonly XNamespace Osis = "http://www.bibletechnologies.net/2003/OSIS/namespace";

    public IEnumerable<StrongsEntry> Parse(string path)
    {
        var doc = XDocument.Load(path);
        foreach (var div in doc.Descendants(Osis + "div").Where(d => (string?)d.Attribute("type") == "entry"))
        {
            var n = (string?)div.Attribute("n");
            if (string.IsNullOrEmpty(n)) continue;
            var w = div.Elements(Osis + "w").FirstOrDefault();

            yield return new StrongsEntry
            {
                Number = n,
                Lemma = w?.Value ?? "",
                Transliteration = (string?)w?.Attribute("xlit") ?? "",
                Pronunciation = (string?)w?.Attribute("POS") ?? "", // placeholder attr; corrected against real data later
                Definition = SectionText(div, "Definition"),
                KjvUsage = SectionText(div, "KJV Usage"),
            };
        }
    }

    private static string SectionText(XElement div, string label)
    {
        var item = div.Descendants(Osis + "item")
            .FirstOrDefault(i => (string?)i.Element(Osis + "label")?.Value == label);
        return item is null ? "" :
            string.Join(' ', item.Elements(Osis + "p").Select(p => p.Value)).Trim();
    }
}
