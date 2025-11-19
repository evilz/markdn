using MarkdownToRazorGenerator.Extensions;
using MarkdownToRazorGenerator.Models;

namespace MarkdownToRazorGenerator.Parsers;

/// <summary>
/// Parses YAML front-matter from Markdown files
/// </summary>
public class FrontMatterParser
{
    /// <summary>
    /// Extracts front-matter and body from markdown content
    /// </summary>
    /// <param name="content">Raw markdown content</param>
    /// <returns>Tuple of (metadata, markdownBody, errors)</returns>
    public (MarkdownMetadata metadata, string markdownBody, List<string> errors) Parse(string content)
    {
        var errors = new List<string>();
        var metadata = new MarkdownMetadata();
        var markdownBody = content;

        if (string.IsNullOrWhiteSpace(content))
        {
            return (metadata, markdownBody, errors);
        }

        // Check if content starts with front-matter delimiter
        if (!content.TrimStart().StartsWith("---"))
        {
            return (metadata, markdownBody, errors);
        }

        try
        {
            // Use the simplified extension method to extract front matter
            metadata = content.GetFrontMatter<MarkdownMetadata>() ?? new MarkdownMetadata();
            markdownBody = content.GetMarkdownBody();
            
            // Get front matter as dictionary to extract variables and parameters with proper types
            var frontMatterDict = content.GetFrontMatter();
            
            if (frontMatterDict != null)
            {
                // Extract variables with proper types from dictionary
                if (frontMatterDict.ContainsKey("variables") && frontMatterDict["variables"] is Dictionary<object, object> vars)
                {
                    metadata.Variables = vars.ToDictionary(
                        kvp => kvp.Key.ToString() ?? "",
                        kvp => kvp.Value
                    );
                }
                
                // Extract parameters with proper types from dictionary
                if (frontMatterDict.ContainsKey("parameters") && frontMatterDict["parameters"] is Dictionary<object, object> parms)
                {
                    metadata.Parameters = parms.ToDictionary(
                        kvp => kvp.Key.ToString() ?? "",
                        kvp => kvp.Value
                    );
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"YAML parsing error: {ex.Message}");
            return (metadata, markdownBody, errors);
        }

        return (metadata, markdownBody, errors);
    }

    /// <summary>
    /// Extracts the first H1 heading from markdown content
    /// </summary>
    public string? ExtractFirstH1(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }

        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# "))
            {
                return trimmed.Substring(2).Trim();
            }
        }

        return null;
    }
}
