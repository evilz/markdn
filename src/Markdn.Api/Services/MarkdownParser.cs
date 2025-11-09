using Markdig;

namespace Markdn.Api.Services;

/// <summary>
/// Service for parsing Markdown to HTML using Markdig with GitHub Flavored Markdown extensions
/// </summary>
public class MarkdownParser
{
    private readonly MarkdownPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the MarkdownParser with GFM pipeline
    /// </summary>
    public MarkdownParser()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes GFM tables, task lists, etc.
            .Build();
    }

    /// <summary>
    /// Parses Markdown content to HTML
    /// </summary>
    /// <param name="markdown">Raw Markdown content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rendered HTML</returns>
    public Task<string> ParseAsync(string markdown, CancellationToken cancellationToken)
    {
        var html = Markdown.ToHtml(markdown, _pipeline);
        return Task.FromResult(html);
    }
}
