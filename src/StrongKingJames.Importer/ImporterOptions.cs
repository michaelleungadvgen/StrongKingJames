using Microsoft.Extensions.Configuration;

namespace StrongKingJames.Importer;

public class ImporterOptions
{
    public string OsisPath { get; set; } = "data/kjv.xml";
    public string HebrewPath { get; set; } = "data/strongshebrew.xml";
    public string GreekPath { get; set; } = "data/strongsgreek.xml";
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

        // Command-line flags take highest precedence.
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            var value = args[i + 1];
            switch (args[i])
            {
                case "--osis": options.OsisPath = value; break;
                case "--hebrew": options.HebrewPath = value; break;
                case "--greek": options.GreekPath = value; break;
                case "--connection": options.ConnectionString = value; break;
                case "--ollama-url": options.OllamaBaseUrl = value; break;
                case "--embedding-model": options.EmbeddingModel = value; break;
            }
        }

        return options;
    }
}
