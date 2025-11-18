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
    private readonly IDeserializer _dynamicDeserializer;

    public FrontMatterParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
            
        // Create a deserializer that attempts to preserve types by trying to convert strings
        // This won't work with YamlDotNet's default behavior, so we'll need to manually convert
        _dynamicDeserializer = new DeserializerBuilder()
            .Build();
    }
    
    /// <summary>
    /// Converts a YAML-deserialized value to its proper .NET type
    /// </summary>
    private object ConvertYamlValue(object value)
    {
        if (value is not string strValue)
        {
            return value; // Already the right type
        }
        
        // Try to convert string to appropriate type
        if (bool.TryParse(strValue, out var boolValue))
        {
            return boolValue;
        }
        
        if (int.TryParse(strValue, out var intValue))
        {
            return intValue;
        }
        
        if (long.TryParse(strValue, out var longValue))
        {
            return longValue;
        }
        
        if (double.TryParse(strValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }
        
        // Return as string if no conversion applies
        return strValue;
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
                        
                        // Second pass: deserialize variables and parameters with type preservation
                        // Deserialize without type constraints
                        var dynamicData = _dynamicDeserializer.Deserialize<object>(yamlContent);
                        
                        if (dynamicData is Dictionary<object, object> dict)
                        {
                            // Extract variables with proper types
                            if (dict.ContainsKey("variables") && dict["variables"] is Dictionary<object, object> vars)
                            {
                                metadata.Variables = vars.ToDictionary(
                                    kvp => kvp.Key.ToString() ?? "",
                                    kvp => ConvertYamlValue(kvp.Value) // Convert to proper type
                                );
                            }
                            
                            // Extract parameters with proper types
                            if (dict.ContainsKey("parameters") && dict["parameters"] is Dictionary<object, object> parms)
                            {
                                metadata.Parameters = parms.ToDictionary(
                                    kvp => kvp.Key.ToString() ?? "",
                                    kvp => ConvertYamlValue(kvp.Value) // Convert to proper type
                                );
                            }
                        }
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
