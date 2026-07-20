using Markdig;

namespace StrongKingJames.Web.Rendering;

/// <summary>
/// Renders assistant/LLM markdown to HTML for display. Raw HTML in the source is disabled,
/// so any literal &lt;script&gt; or tags in model output are escaped and shown as text rather
/// than executed — safe to emit via a Blazor MarkupString.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .UsePipeTables()
        .Build();

    public static string ToHtml(string? markdown) =>
        string.IsNullOrEmpty(markdown) ? "" : Markdown.ToHtml(markdown, Pipeline);
}
