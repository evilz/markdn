using Markdig;

namespace MarkdownToRazorGenerator.Parsers;

/// <summary>
/// Converts Markdown content to HTML using Markdig
/// </summary>
public class MarkdownConverter
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownConverter()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Converts markdown to HTML
    /// </summary>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, _pipeline);
    }
}
