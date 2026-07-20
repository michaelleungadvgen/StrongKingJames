using Microsoft.Extensions.Configuration;

namespace StrongKingJames.Importer;

public class ImporterOptions
{
    // "kjs" (default; has Strong's for BOTH testaments) or "osis" — selects the Bible/Strong's source format.
    public string Format { get; set; } = "kjs";

    // OSIS source paths (format = osis).
    public string OsisPath { get; set; } = "data/kjv.xml";
    public string HebrewPath { get; set; } = "data/strongshebrew.xml";
    public string GreekPath { get; set; } = "data/strongsgreek.xml";

    // kjs JSON source paths (format = kjs). Files from https://github.com/1John419/kjs/json.
    // kjv_pure.json + strong_pure.json give the KJV text with per-word Strong's for both
    // testaments (OT Hebrew + NT Greek); strong_dict.json is the matching Hebrew+Greek lexicon.
    public string KjsBiblePath { get; set; } = "data/kjv_pure.json";
    public string KjsStrongsPath { get; set; } = "data/strong_pure.json";
    public string KjsStrongsDictPath { get; set; } = "data/strong_dict.json";

    public string ConnectionString { get; set; } =
        "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    public static ImporterOptions Parse(string[] args, IConfiguration config)
    {
        var options = new ImporterOptions();

        // Layer in configuration (env vars, appsettings, Aspire-injected connection strings).
        var connFromConfig = config.GetConnectionString("bible");
        if (!string.IsNullOrWhiteSpace(connFromConfig))
            options.ConnectionString = connFromConfig;

        var ollamaFromConfig = config["Ollama:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(ollamaFromConfig))
            options.OllamaBaseUrl = ollamaFromConfig;

        var formatFromConfig = config["Importer:Format"];
        if (!string.IsNullOrWhiteSpace(formatFromConfig))
            options.Format = formatFromConfig;

        var kjsBibleFromConfig = config["Importer:KjsBiblePath"];
        if (!string.IsNullOrWhiteSpace(kjsBibleFromConfig))
            options.KjsBiblePath = kjsBibleFromConfig;

        var kjsStrongsFromConfig = config["Importer:KjsStrongsPath"];
        if (!string.IsNullOrWhiteSpace(kjsStrongsFromConfig))
            options.KjsStrongsPath = kjsStrongsFromConfig;

        var kjsDictFromConfig = config["Importer:KjsStrongsDictPath"];
        if (!string.IsNullOrWhiteSpace(kjsDictFromConfig))
            options.KjsStrongsDictPath = kjsDictFromConfig;

        var osisFromConfig = config["Importer:OsisPath"];
        if (!string.IsNullOrWhiteSpace(osisFromConfig))
            options.OsisPath = osisFromConfig;

        var hebrewFromConfig = config["Importer:HebrewPath"];
        if (!string.IsNullOrWhiteSpace(hebrewFromConfig))
            options.HebrewPath = hebrewFromConfig;

        var greekFromConfig = config["Importer:GreekPath"];
        if (!string.IsNullOrWhiteSpace(greekFromConfig))
            options.GreekPath = greekFromConfig;

        // Command-line flags take highest precedence.
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            var value = args[i + 1];
            switch (args[i])
            {
                case "--format": options.Format = value; break;
                case "--osis": options.OsisPath = value; break;
                case "--hebrew": options.HebrewPath = value; break;
                case "--greek": options.GreekPath = value; break;
                case "--kjs-bible": options.KjsBiblePath = value; break;
                case "--kjs-strongs": options.KjsStrongsPath = value; break;
                case "--kjs-dict": options.KjsStrongsDictPath = value; break;
                case "--connection": options.ConnectionString = value; break;
                case "--ollama-url": options.OllamaBaseUrl = value; break;
                case "--embedding-model": options.EmbeddingModel = value; break;
            }
        }

        return options;
    }
}
