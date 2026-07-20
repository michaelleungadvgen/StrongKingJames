using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Rag;

/// <summary>
/// Builds the chat prompt that asks the model to explain a Strong's Concordance entry
/// in plain language for a curious reader.
/// </summary>
public static class StrongsExplainPromptBuilder
{
    private const string SystemPrompt =
        "You are a Bible study assistant explaining a Strong's Concordance entry to a curious " +
        "layperson. Explain the original Hebrew or Greek word clearly and concisely: its core " +
        "meaning, nuances, and how it is used across the King James Version. Keep it to a few " +
        "short paragraphs in plain English. Base your explanation on the entry provided and your " +
        "general knowledge of biblical languages; do not fabricate scripture quotations.";

    public static IReadOnlyList<ChatMessage> Build(StrongsEntry entry)
    {
        var user =
            $"Explain this Strong's entry:\n" +
            $"Number: {entry.Number}\n" +
            $"Original word: {entry.Lemma}\n" +
            $"Transliteration: {entry.Transliteration}\n" +
            $"Pronunciation: {entry.Pronunciation}\n" +
            $"Definition: {entry.Definition}\n" +
            $"KJV usage: {entry.KjvUsage}";

        return
        [
            new ChatMessage("system", SystemPrompt),
            new ChatMessage("user", user),
        ];
    }
}
