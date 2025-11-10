using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdn.SourceGenerators.Models;

namespace Markdn.SourceGenerators.Parsers;

/// <summary>
/// Basic YAML front matter parser for Markdown files.
/// Parses --- delimited YAML at the start of files into ComponentMetadata.
/// Zero external dependencies - implements subset of YAML needed for front matter.
/// </summary>
public static class YamlFrontMatterParser
{
    private const string Delimiter = "---";

    /// <summary>
    /// Parse YAML front matter and return metadata + remaining Markdown content.
    /// </summary>
    /// <param name="content">Full Markdown file content</param>
    /// <returns>Tuple of (ComponentMetadata, remaining Markdown content)</returns>
    public static (ComponentMetadata Metadata, string MarkdownContent) Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (ComponentMetadata.Empty, content ?? string.Empty);
        }

        // Check if content starts with YAML front matter delimiter
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        if (lines.Length < 3 || lines[0].Trim() != Delimiter)
        {
            // No YAML front matter found
            return (ComponentMetadata.Empty, content);
        }

        // Find closing delimiter
        int closingDelimiterIndex = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == Delimiter)
            {
                closingDelimiterIndex = i;
                break;
            }
        }

        if (closingDelimiterIndex == -1)
        {
            // No closing delimiter found - treat as regular content
            return (ComponentMetadata.Empty, content);
        }

        // Extract YAML content (between delimiters)
        var yamlLines = lines.Skip(1).Take(closingDelimiterIndex - 1).ToArray();
        var yamlContent = string.Join("\n", yamlLines);

        // Extract remaining Markdown content (after closing delimiter)
        var markdownLines = lines.Skip(closingDelimiterIndex + 1);
        var markdownContent = string.Join("\n", markdownLines);

        // Parse YAML into ComponentMetadata
        var metadata = ParseYaml(yamlContent);

        return (metadata, markdownContent);
    }

    /// <summary>
    /// Parse YAML content into ComponentMetadata.
    /// Supports basic key-value pairs and arrays.
    /// </summary>
    private static ComponentMetadata ParseYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return ComponentMetadata.Empty;
        }

        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var lines = yaml.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                continue;
            }

            // Parse key-value pair
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
            {
                continue; // Invalid line, skip
            }

            var key = line.Substring(0, colonIndex).Trim();
            var valueText = line.Substring(colonIndex + 1).Trim();

            // Check if this is an array (value is empty and next lines are indented with -)
            if (string.IsNullOrEmpty(valueText) && i + 1 < lines.Length)
            {
                var arrayValues = new List<string>();
                int j = i + 1;
                
                while (j < lines.Length)
                {
                    var nextLine = lines[j];
                    if (string.IsNullOrWhiteSpace(nextLine))
                    {
                        j++;
                        continue;
                    }

                    var trimmedNextLine = nextLine.TrimStart();
                    if (trimmedNextLine.StartsWith("-"))
                    {
                        // Array item
                        var itemValue = trimmedNextLine.Substring(1).Trim();
                        
                        // Remove quotes if present
                        if ((itemValue.StartsWith("\"") && itemValue.EndsWith("\"")) ||
                            (itemValue.StartsWith("'") && itemValue.EndsWith("'")))
                        {
                            itemValue = itemValue.Substring(1, itemValue.Length - 2);
                        }
                        
                        arrayValues.Add(itemValue);
                        j++;
                    }
                    else
                    {
                        // Next property or end of array
                        break;
                    }
                }
                
                if (arrayValues.Count > 0)
                {
                    properties[key] = arrayValues;
                    i = j - 1; // Skip processed lines
                }
            }
            else
            {
                // Scalar value
                var value = valueText;
                
                // Remove quotes if present
                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                
                properties[key] = value;
            }
        }

        // Map to ComponentMetadata
        var url = GetStringValue(properties, "url");
        var urlArray = GetArrayValue(properties, "url")?.ToArray();
        
        // Validation: Url and UrlArray are mutually exclusive (will be reported as MD008 by generator)
        // For now, if both are present, prioritize scalar Url
        if (url != null && urlArray != null)
        {
            urlArray = null; // Ignore array if scalar is present
        }
        
        // Validation: URLs must start with / (will be reported as MD002 by generator)
        // This validation will be performed in the generator where diagnostics can be emitted
        
        return new ComponentMetadata
        {
            Url = url,
            UrlArray = urlArray,
            Title = GetStringValue(properties, "title"),
            Namespace = GetStringValue(properties, "namespace"),
            Using = GetArrayValue(properties, "using")?.ToArray(),
            Layout = GetStringValue(properties, "layout"),
            Inherit = GetStringValue(properties, "inherit"),
            Attribute = GetArrayValue(properties, "attribute")?.ToArray(),
            Parameters = ParseParameters(properties)
        };
    }

    /// <summary>
    /// Get string value from properties dictionary.
    /// </summary>
    private static string? GetStringValue(Dictionary<string, object> properties, string key)
    {
        if (properties.TryGetValue(key, out var value))
        {
            // If it's a string, return it
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Get array value from properties dictionary.
    /// </summary>
    private static List<string>? GetArrayValue(Dictionary<string, object> properties, string key)
    {
        if (properties.TryGetValue(key, out var value))
        {
            // If it's already a list, return it
            if (value is List<string> listValue && listValue.Count > 0)
            {
                return listValue;
            }
            
            // If it's a single string value, wrap it in a list
            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                return new List<string> { stringValue };
            }
        }
        
        return null;
    }

    /// <summary>
    /// Parse parameters array from properties dictionary.
    /// Expected format:
    /// parameters:
    ///   - name: Title
    ///     type: string
    ///   - name: Count
    ///     type: int
    /// </summary>
    private static List<ParameterDefinition>? ParseParameters(Dictionary<string, object> properties)
    {
        // Parameters parsing would require more complex YAML parsing
        // For now, return null - will be implemented in later tasks (T084)
        return null;
    }
}
