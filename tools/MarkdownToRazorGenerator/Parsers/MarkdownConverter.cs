using Markdig;
using System.Text.RegularExpressions;

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

        var html = Markdown.ToHtml(markdown, _pipeline);
        
        // Post-process to unescape HTML entities within Razor expressions
        return UnescapeRazorExpressions(html);
    }

    /// <summary>
    /// Unescapes HTML entities within Razor expressions (@...) to preserve valid Razor syntax
    /// </summary>
    private string UnescapeRazorExpressions(string html)
    {
        // Match Razor expressions: @ followed by any characters until whitespace, newline, or HTML tag
        // This pattern captures expressions like @DateTime.Now.ToString("HH:mm:ss") or @Model.Name
        var pattern = @"@([A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*(\([^)]*\))*)";
        
        return Regex.Replace(html, pattern, match =>
        {
            var expression = match.Value;
            // Unescape HTML entities within the Razor expression
            expression = expression.Replace("&quot;", "\"")
                                  .Replace("&#39;", "'")
                                  .Replace("&lt;", "<")
                                  .Replace("&gt;", ">")
                                  .Replace("&amp;", "&");
            return expression;
        });
    }
}
