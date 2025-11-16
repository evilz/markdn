using MarkdownToRazorGenerator.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MarkdownToRazorGenerator.Parsers;

/// <summary>
/// Parses YAML front-matter from Markdown files
/// </summary>
public class FrontMatterParser
{
    private readonly IDeserializer _deserializer;

    public FrontMatterParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

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
            // Find the second --- delimiter
            var lines = content.Split('\n');
            int firstDelimiterLine = -1;
            int secondDelimiterLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                var trimmedLine = lines[i].Trim();
                if (trimmedLine == "---")
                {
                    if (firstDelimiterLine == -1)
                    {
                        firstDelimiterLine = i;
                    }
                    else
                    {
                        secondDelimiterLine = i;
                        break;
                    }
                }
            }

            if (firstDelimiterLine != -1 && secondDelimiterLine != -1 && secondDelimiterLine > firstDelimiterLine)
            {
                // Extract YAML content between delimiters
                var yamlLines = lines.Skip(firstDelimiterLine + 1).Take(secondDelimiterLine - firstDelimiterLine - 1);
                var yamlContent = string.Join('\n', yamlLines);

                // Extract markdown body after second delimiter
                var bodyLines = lines.Skip(secondDelimiterLine + 1);
                markdownBody = string.Join('\n', bodyLines);

                // Parse YAML
                if (!string.IsNullOrWhiteSpace(yamlContent))
                {
                    try
                    {
                        metadata = _deserializer.Deserialize<MarkdownMetadata>(yamlContent) ?? new MarkdownMetadata();
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"YAML parsing error: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Front-matter extraction error: {ex.Message}");
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
