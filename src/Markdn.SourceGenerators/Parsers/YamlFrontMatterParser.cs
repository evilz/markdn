using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdn.SourceGenerators.Models;

namespace Markdn.SourceGenerators.Parsers;

/// <summary>
/// YAML front matter parser for Markdown files using Markdig's YAML extension for block detection
/// and a minimal YAML subset parser for mapping to ComponentMetadata.
/// </summary>
public static class YamlFrontMatterParser
{
    private const string Delimiter = "---";

    /// <summary>
    /// Parse YAML front matter and return metadata + remaining Markdown content.
    /// </summary>
    /// <param name="content">Full Markdown file content</param>
    /// <returns>Tuple of (ComponentMetadata, remaining Markdown content, parsing errors)</returns>
    public static (ComponentMetadata Metadata, string MarkdownContent, List<string> Errors) Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (ComponentMetadata.Empty, content ?? string.Empty, new List<string>());
        }

        var errors = new List<string>();

        // Pre-validate: if file starts with '---', ensure there's a closing '---'
        var allLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (allLines.Length > 0 && string.Equals(allLines[0].Trim(), Delimiter, StringComparison.Ordinal))
        {
            bool hasClosing = false;
            for (int i = 1; i < allLines.Length; i++)
            {
                if (string.Equals(allLines[i].Trim(), Delimiter, StringComparison.Ordinal))
                {
                    hasClosing = true;
                    break;
                }
            }
            if (!hasClosing)
            {
                errors.Add("Invalid YAML front matter: missing closing '---' delimiter");
                return (ComponentMetadata.Empty, content, errors);
            }
        }

        // Build Markdig pipeline with YAML front matter support
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();

        // Parse document
        var document = Markdown.Parse(content, pipeline);

        // Locate YAML front matter block if present
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (yamlBlock == null)
        {
            // No front matter -> return original content
            return (ComponentMetadata.Empty, content, errors);
        }

        // Extract YAML content lines from the block
        var yamlBuilder = new StringBuilder();
        for (int i = 0; i < yamlBlock.Lines.Count; i++)
        {
            var line = yamlBlock.Lines.Lines[i];
            yamlBuilder.AppendLine(line.Slice.ToString());
        }
        var yamlText = yamlBuilder.ToString();

        // Parse YAML into ComponentMetadata (custom minimal mapping)
        var (metadata, yamlErrors) = ParseYaml(yamlText);
        if (yamlErrors.Count > 0)
        {
            errors.AddRange(yamlErrors);
        }

        // Extract remaining Markdown content: take everything after the second '---' delimiter
        int delimiterCount = 0;
        int startLine = 0;
        for (int i = 0; i < allLines.Length; i++)
        {
            if (string.Equals(allLines[i].Trim(), Delimiter, StringComparison.Ordinal))
            {
                delimiterCount++;
                if (delimiterCount == 2)
                {
                    startLine = i + 1;
                    break;
                }
            }
        }
        var markdownContent = startLine < allLines.Length
            ? string.Join("\n", allLines.Skip(startLine))
            : string.Empty;

        return (metadata, markdownContent, errors);
    }

    /// <summary>
    /// Parse YAML content into ComponentMetadata.
    /// Supports basic key-value pairs, arrays of scalars, and a special 'parameters' array of objects.
    /// </summary>
    private static (ComponentMetadata, List<string>) ParseYaml(string yaml)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return (ComponentMetadata.Empty, errors);
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

            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
            {
                errors.Add($"Invalid YAML syntax: missing ':' in line '{line.Trim()}'");
                continue;
            }

            var key = line.Substring(0, colonIndex).Trim();
            var valueText = line.Substring(colonIndex + 1).Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"Invalid YAML syntax: empty key in line '{line.Trim()}'");
                continue;
            }

            // Arrays
            if (string.IsNullOrEmpty(valueText) && i + 1 < lines.Length)
            {
                if (key.Equals("parameters", StringComparison.OrdinalIgnoreCase) || key.Equals("$parameters", StringComparison.OrdinalIgnoreCase))
                {
                    var (parametersList, nextIndex) = ParseParameterArray(lines, i + 1);
                    if (parametersList.Count > 0)
                    {
                        properties[key] = parametersList;
                        i = nextIndex - 1;
                    }
                }
                else
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
                        var trimmed = nextLine.TrimStart();
                        if (trimmed.StartsWith("-"))
                        {
                            var item = trimmed.Substring(1).Trim();
                            if ((item.StartsWith("\"") && item.EndsWith("\"")) || (item.StartsWith("'") && item.EndsWith("'")))
                            {
                                item = item.Substring(1, item.Length - 2);
                            }
                            arrayValues.Add(item);
                            j++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (arrayValues.Count > 0)
                    {
                        properties[key] = arrayValues;
                        i = j - 1;
                    }
                }
            }
            else
            {
                // Scalar value
                var value = valueText;
                if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                properties[key] = value;
            }
        }

        var metadata = new ComponentMetadata
        {
            Title = GetStringValue(properties, "title"),
            Namespace = GetStringValue(properties, "namespace", "$namespace"),
            Using = GetArrayValue(properties, "using", "$using")?.ToArray(),
            ComponentNamespaces = GetArrayValue(properties, "componentNamespaces", "$componentNamespaces")?.ToArray(),
            Layout = GetStringValue(properties, "layout", "$layout"),
            Inherit = GetStringValue(properties, "inherit", "$inherit"),
            Attribute = GetArrayValue(properties, "attribute", "$attribute")?.ToArray(),
            Slug = GetStringValue(properties, "slug"),
            Parameters = ParseParameters(properties)
        };

        return (metadata, errors);
    }

    private static string? GetStringValue(Dictionary<string, object> properties, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.TryGetValue(key, out var value) && value is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        return null;
    }

    private static List<string>? GetArrayValue(Dictionary<string, object> properties, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is List<string> list && list.Count > 0)
                    return list;

                if (value is string s && !string.IsNullOrWhiteSpace(s))
                    return new List<string> { s };
            }
        }
        return null;
    }

    private static IReadOnlyList<ParameterDefinition>? ParseParameters(Dictionary<string, object> properties)
    {
        if (!properties.TryGetValue("parameters", out var p) || p is not List<Dictionary<string, string>> arr || arr.Count == 0)
        {
            return null;
        }

        var list = new List<ParameterDefinition>(arr.Count);
        foreach (var obj in arr)
        {
            obj.TryGetValue("name", out var name);
            obj.TryGetValue("type", out var type);
            list.Add(new ParameterDefinition
            {
                Name = name ?? string.Empty,
                Type = type ?? string.Empty
            });
        }
        return list;
    }

    private static (List<Dictionary<string, string>> Items, int NextIndex) ParseParameterArray(string[] lines, int startIndex)
    {
        var items = new List<Dictionary<string, string>>();
        int i = startIndex;
        while (i < lines.Length)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("-"))
            {
                break; // end of array
            }

            // Start new object
            var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Support inline: - name: X
            var rest = trimmed.Substring(1).Trim();
            if (!string.IsNullOrEmpty(rest))
            {
                var idx = rest.IndexOf(':');
                if (idx > 0)
                {
                    var k = rest.Substring(0, idx).Trim();
                    var v = rest.Substring(idx + 1).Trim();
                    if ((v.StartsWith("\"") && v.EndsWith("\"")) || (v.StartsWith("'") && v.EndsWith("'")))
                    {
                        v = v.Substring(1, v.Length - 2);
                    }
                    current[k] = v;
                }
            }

            i++;
            // Parse indented key-value pairs under this dash item
            while (i < lines.Length)
            {
                var l = lines[i];
                if (string.IsNullOrWhiteSpace(l))
                {
                    i++;
                    continue;
                }
                var lTrim = l.TrimStart();
                if (lTrim.StartsWith("-"))
                {
                    break; // next array item
                }
                var cidx = l.IndexOf(':');
                if (cidx == -1)
                {
                    // malformed line
                    i++;
                    continue;
                }
                var k2 = l.Substring(0, cidx).Trim();
                var v2 = l.Substring(cidx + 1).Trim();
                if ((v2.StartsWith("\"") && v2.EndsWith("\"")) || (v2.StartsWith("'") && v2.EndsWith("'")))
                {
                    v2 = v2.Substring(1, v2.Length - 2);
                }
                if (!string.IsNullOrWhiteSpace(k2))
                {
                    current[k2] = v2;
                }
                i++;
            }

            items.Add(current);
        }
        return (items, i);
    }
}
