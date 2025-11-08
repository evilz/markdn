using Markdig;

namespace Markdn.Api.Services;

/// <summary>
/// Service for parsing Markdown to HTML
/// </summary>
public class MarkdownParser
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownParser()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Includes GFM tables, task lists, etc.
            .Build();
    }

    public Task<string> ParseAsync(string markdown, CancellationToken cancellationToken)
    {
        var html = Markdown.ToHtml(markdown, _pipeline);
        return Task.FromResult(html);
    }
}
