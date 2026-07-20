using StrongKingJames.Core.Models;
using StrongKingJames.Core.Rag;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class RagPromptBuilderTests
{
    [Fact]
    public void Build_includes_system_user_and_passages_and_question()
    {
        var passages = new[]
        {
            new RetrievedPassage("John 3:16", "For God so loved the world...", 0.9),
            new RetrievedPassage("1 John 4:8", "God is love.", 0.8),
        };

        var messages = RagPromptBuilder.Build("What is love?", passages);

        Assert.Equal(2, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Contains("cite", messages[0].Content, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal("user", messages[1].Role);
        Assert.Contains("John 3:16", messages[1].Content);
        Assert.Contains("God is love.", messages[1].Content);
        Assert.Contains("What is love?", messages[1].Content);
    }

    [Fact]
    public void Build_with_no_passages_still_produces_user_message()
    {
        var messages = RagPromptBuilder.Build("anything", System.Array.Empty<RetrievedPassage>());
        Assert.Equal(2, messages.Count);
        Assert.Contains("anything", messages[1].Content);
    }
}
