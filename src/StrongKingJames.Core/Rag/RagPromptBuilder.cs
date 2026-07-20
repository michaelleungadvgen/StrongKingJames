using System.Text;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Rag;

public static class RagPromptBuilder
{
    private const string SystemPrompt =
        "You are a knowledgeable Bible study assistant. Use the scripture passages provided " +
        "below as your primary source, and cite the passages you rely on by their reference " +
        "in the form Book chapter:verse (e.g. John 3:16). If the passages do not fully answer " +
        "the question, you may also draw on your broader knowledge of the Bible, its people, " +
        "and Christian tradition to give a helpful answer — but make clear which parts go " +
        "beyond the provided passages, and never fabricate direct scripture quotations or " +
        "invent verse references. If the passages are empty or unrelated, answer from your " +
        "general knowledge of the Bible.";

    public static IReadOnlyList<ChatMessage> Build(
        string question,
        IReadOnlyList<RetrievedPassage> passages,
        IReadOnlyList<ChatMessage>? history = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Passages:");
        foreach (var p in passages)
        {
            sb.AppendLine($"[{p.Reference}] {p.Text}");
        }
        sb.AppendLine();
        sb.AppendLine($"Question: {question}");

        // system prompt, then prior conversation turns, then the current question with its passages.
        var messages = new List<ChatMessage> { new("system", SystemPrompt) };
        if (history is not null)
            messages.AddRange(history);
        messages.Add(new ChatMessage("user", sb.ToString()));
        return messages;
    }
}
