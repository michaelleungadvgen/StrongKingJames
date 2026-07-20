namespace StrongKingJames.Core.Ollama;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    // Pinned to the tag installed on the host; bare "llama3.1" resolves to ":latest" which may not be pulled.
    public string ChatModel { get; set; } = "llama3.1:8b";
}
