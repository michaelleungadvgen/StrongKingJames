using System.Text;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Rag;

public static class RagPromptBuilder
{
    private const string SystemPrompt =
        "You are a Bible study assistant. Answer the user's question using ONLY the " +
        "scripture passages provided below. Cite every claim with its reference in the " +
        "form Book chapter:verse (e.g. John 3:16). If the passages do not answer the " +
        "question, say so plainly and do not invent scripture.";

    public static IReadOnlyList<ChatMessage> Build(
        string question, IReadOnlyList<RetrievedPassage> passages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Passages:");
        foreach (var p in passages)
        {
            sb.AppendLine($"[{p.Reference}] {p.Text}");
        }
        sb.AppendLine();
        sb.AppendLine($"Question: {question}");

        return
        [
            new ChatMessage("system", SystemPrompt),
            new ChatMessage("user", sb.ToString()),
        ];
    }
}
