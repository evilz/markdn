using Markdig;
using System.Text;
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
    /// Converts markdown to HTML, processing Section tags for Blazor SectionContent components
    /// Returns a tuple of (main content HTML, list of section contents)
    /// </summary>
    public (string mainContent, List<SectionInfo> sections) ToHtmlWithSections(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return (string.Empty, new List<SectionInfo>());
        }

        // First, identify and protect code blocks so we don't parse Section tags inside them
        var codeBlockRanges = new List<(int start, int end)>();
        
        // Find fenced code blocks (```)
        var fencedPattern = @"```[\s\S]*?```";
        var fencedMatches = Regex.Matches(markdown, fencedPattern);
        foreach (Match match in fencedMatches)
        {
            codeBlockRanges.Add((match.Index, match.Index + match.Length));
        }
        
        // Find inline code (`...`)
        var inlinePattern = @"`[^`]+`";
        var inlineMatches = Regex.Matches(markdown, inlinePattern);
        foreach (Match match in inlineMatches)
        {
            codeBlockRanges.Add((match.Index, match.Index + match.Length));
        }
        
        // Sort ranges by start position
        codeBlockRanges = codeBlockRanges.OrderBy(r => r.start).ToList();
        
        // Helper function to check if a position is inside a code block
        bool IsInCodeBlock(int position)
        {
            return codeBlockRanges.Any(range => position >= range.start && position < range.end);
        }
        
        var sections = new List<SectionInfo>();
        var mainContent = new StringBuilder();
        
        int position = 0;
        while (position < markdown.Length)
        {
            // Find next section start tag (case-insensitive)
            var sectionStartPattern = @"<Section\s+Name\s*=\s*[""']([^""']+)[""']\s*>";
            var match = Regex.Match(markdown.Substring(position), sectionStartPattern, RegexOptions.IgnoreCase);
            
            if (!match.Success)
            {
                // No more sections, add rest to main content
                mainContent.Append(markdown.Substring(position));
                break;
            }
            
            var matchStartPos = position + match.Index;
            
            // Check if this match is inside a code block - if so, skip it
            if (IsInCodeBlock(matchStartPos))
            {
                // Add up to end of this match to main content and continue
                mainContent.Append(markdown.Substring(position, match.Index + match.Length));
                position += match.Index + match.Length;
                continue;
            }
            
            // Add text before section to main content
            mainContent.Append(markdown.Substring(position, match.Index));
            
            var sectionName = match.Groups[1].Value;
            var sectionStartPos = position + match.Index + match.Length;
            
            // Find matching end tag for this section (not in a code block)
            var endTagPattern = @"</Section\s*>";
            int searchPos = sectionStartPos;
            Match? endMatch = null;
            
            while (searchPos < markdown.Length)
            {
                var tempMatch = Regex.Match(markdown.Substring(searchPos), endTagPattern, RegexOptions.IgnoreCase);
                if (!tempMatch.Success)
                {
                    break;
                }
                
                var endMatchPos = searchPos + tempMatch.Index;
                
                // Check if this end tag is inside a code block
                if (!IsInCodeBlock(endMatchPos))
                {
                    endMatch = tempMatch;
                    break;
                }
                
                // Skip this match and continue searching
                searchPos = endMatchPos + tempMatch.Length;
            }
            
            if (endMatch == null)
            {
                // No closing tag found, treat as regular content
                mainContent.Append(markdown.Substring(position + match.Index));
                position = markdown.Length;
                break;
            }
            
            var sectionContent = markdown.Substring(sectionStartPos, searchPos + endMatch.Index - sectionStartPos).Trim();
            
            // Convert section markdown to HTML
            var sectionHtml = Markdown.ToHtml(sectionContent, _pipeline);
            sectionHtml = UnescapeRazorExpressions(sectionHtml);
            
            sections.Add(new SectionInfo
            {
                Name = sectionName,
                Content = sectionHtml
            });
            
            // Move position past the closing tag
            position = searchPos + endMatch.Index + endMatch.Length;
        }

        // Convert main content to HTML
        var mainHtml = Markdown.ToHtml(mainContent.ToString().Trim(), _pipeline);
        mainHtml = UnescapeRazorExpressions(mainHtml);

        return (mainHtml, sections);
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

/// <summary>
/// Represents a section extracted from markdown
/// </summary>
public class SectionInfo
{
    public required string Name { get; set; }
    public required string Content { get; set; }
}
