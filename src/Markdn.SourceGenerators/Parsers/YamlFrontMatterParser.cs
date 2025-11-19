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
    /// <returns>Tuple of (ComponentMetadata, remaining Markdown content, parsing errors)</returns>
    public static (ComponentMetadata Metadata, string MarkdownContent, List<string> Errors) Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (ComponentMetadata.Empty, content ?? string.Empty, new List<string>());
        }

        // Check if content starts with YAML front matter delimiter
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        if (lines.Length < 3 || lines[0].Trim() != Delimiter)
        {
            // No YAML front matter found
            return (ComponentMetadata.Empty, content, new List<string>());
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
            // No closing delimiter found - report invalid YAML front matter (MD001)
            var yamlErrors = new List<string>
            {
                "Invalid YAML front matter: missing closing '---' delimiter"
            };

            // Treat whole file as markdown content when front matter is malformed
            return (ComponentMetadata.Empty, content, yamlErrors);
        }

        // Extract YAML content (between delimiters)
        var yamlLines = lines.Skip(1).Take(closingDelimiterIndex - 1).ToArray();
        var yamlContent = string.Join("\n", yamlLines);

        // Extract remaining Markdown content (after closing delimiter)
        var markdownLines = lines.Skip(closingDelimiterIndex + 1);
        var markdownContent = string.Join("\n", markdownLines);

        // Parse YAML into ComponentMetadata
        var (metadata, errors) = ParseYaml(yamlContent);

        return (metadata, markdownContent, errors);
    }

    /// <summary>
    /// Parse YAML content into ComponentMetadata.
    /// Supports basic key-value pairs, arrays, nested objects, and YAML anchors/aliases.
    /// </summary>
    private static (ComponentMetadata, List<string>) ParseYaml(string yaml)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return (ComponentMetadata.Empty, errors);
        }

        var anchors = new Dictionary<string, object>(StringComparer.Ordinal);
        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var lines = yaml.Split('\n');
        
        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                i++;
                continue;
            }

            // Get indentation level
            var indent = GetIndentation(line);
            
            // Only process top-level keys here (indent == 0)
            if (indent > 0)
            {
                i++;
                continue;
            }

            // Parse key-value pair
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
            {
                // Invalid YAML line - no colon found
                errors.Add($"Invalid YAML syntax: missing ':' in line '{line.Trim()}'");
                i++;
                continue;
            }

            var key = line.Substring(0, colonIndex).Trim();
            var valueText = line.Substring(colonIndex + 1).Trim();

            // Check for YAML anchor definition (e.g., "bill-to: &id001")
            string? anchorName = null;
            if (valueText.StartsWith("&"))
            {
                var spaceIdx = valueText.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    anchorName = valueText.Substring(1, spaceIdx - 1);
                    valueText = valueText.Substring(spaceIdx + 1).Trim();
                }
                else
                {
                    anchorName = valueText.Substring(1);
                    valueText = "";
                }
            }

            // Validate key is not empty
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"Invalid YAML syntax: empty key in line '{line.Trim()}'");
                i++;
                continue;
            }

            // Check for alias reference (e.g., "ship-to: *id001")
            if (valueText.StartsWith("*"))
            {
                var aliasName = valueText.Substring(1).Trim();
                if (anchors.TryGetValue(aliasName, out var aliasValue))
                {
                    properties[key] = aliasValue;
                }
                else
                {
                    errors.Add($"YAML anchor reference '*{aliasName}' not found");
                }
                i++;
                continue;
            }

            // Check if value is empty - indicates nested content
            if (string.IsNullOrEmpty(valueText))
            {
                // Look ahead to see what kind of content follows
                var (nextValue, nextIndex) = ParseNestedValue(lines, i + 1, indent);
                
                if (nextValue != null)
                {
                    // Special handling for known keys
                    if (key.Equals("parameters", StringComparison.OrdinalIgnoreCase) || 
                        key.Equals("$parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        // Parameters are special - convert to ParameterDefinition list
                        properties[key] = nextValue;
                    }
                    else
                    {
                        properties[key] = nextValue;
                        
                        // Store anchor if present
                        if (anchorName != null)
                        {
                            anchors[anchorName] = nextValue;
                        }
                    }
                    i = nextIndex - 1;
                }
            }
            else if (valueText.StartsWith("|") || valueText.StartsWith(">"))
            {
                // Multi-line string (literal or folded)
                var (multiLineValue, nextIndex) = ParseMultiLineString(lines, i + 1, indent);
                properties[key] = multiLineValue;
                i = nextIndex - 1;
            }
            else
            {
                // Scalar value
                var value = ParseScalarValue(valueText);
                properties[key] = value;
                
                // Store anchor if present
                if (anchorName != null)
                {
                    anchors[anchorName] = value;
                }
            }
            
            i++;
        }

        // Separate variables from metadata keys
        var reservedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "title", "namespace", "$namespace", "using", "$using", 
            "componentNamespaces", "$componentNamespaces", "layout", "$layout",
            "inherit", "$inherit", "attribute", "$attribute", "slug", "url",
            "parameters", "$parameters", "variables"
        };
        
        var variables = new Dictionary<string, object>(StringComparer.Ordinal);
        
        // If "variables" key exists, use its content as the variables
        if (properties.TryGetValue("variables", out var variablesObj) && variablesObj is Dictionary<string, object> varsDict)
        {
            foreach (var kvp in varsDict)
            {
                variables[kvp.Key] = kvp.Value;
            }
        }
        
        // Also include any top-level non-reserved keys as variables
        foreach (var kvp in properties)
        {
            if (!reservedKeys.Contains(kvp.Key))
            {
                variables[kvp.Key] = kvp.Value;
            }
        }

        // Map to ComponentMetadata
        var metadata = new ComponentMetadata
        {
            Title = GetStringValue(properties, "title"),
            Namespace = GetStringValue(properties, "namespace", "$namespace"),
            Using = GetArrayValue(properties, "using", "$using")?.ToArray(),
            ComponentNamespaces = GetArrayValue(properties, "componentNamespaces", "$componentNamespaces")?.ToArray(),
            Layout = GetStringValue(properties, "layout", "$layout"),
            Inherit = GetStringValue(properties, "inherit", "$inherit"),
            Attribute = GetArrayValue(properties, "attribute", "$attribute")?.ToArray(),
            Slug = GetStringValue(properties, "slug", "url"),
            Parameters = ParseParameters(properties),
            Variables = variables.Count > 0 ? variables : null
        };
        
        return (metadata, errors);
    }

    /// <summary>
    /// Get indentation level (number of leading spaces)
    /// </summary>
    private static int GetIndentation(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == ' ')
            {
                count++;
            }
            else if (c == '\t')
            {
                count += 4; // Treat tab as 4 spaces
            }
            else
            {
                break;
            }
        }
        return count;
    }

    /// <summary>
    /// Parse a nested value (object or array) starting at the given index
    /// </summary>
    private static (object? value, int nextIndex) ParseNestedValue(string[] lines, int startIndex, int parentIndent)
    {
        if (startIndex >= lines.Length)
        {
            return (null, startIndex);
        }

        // Skip empty lines
        while (startIndex < lines.Length && string.IsNullOrWhiteSpace(lines[startIndex]))
        {
            startIndex++;
        }

        if (startIndex >= lines.Length)
        {
            return (null, startIndex);
        }

        var firstLine = lines[startIndex];
        var firstLineIndent = GetIndentation(firstLine);
        
        // Must be indented relative to parent
        if (firstLineIndent <= parentIndent)
        {
            return (null, startIndex);
        }

        var trimmed = firstLine.TrimStart();
        
        // Check if it's an array (starts with -)
        if (trimmed.StartsWith("-"))
        {
            return ParseArray(lines, startIndex, parentIndent);
        }
        else if (trimmed.Contains(":"))
        {
            // It's an object
            return ParseObject(lines, startIndex, parentIndent);
        }
        
        return (null, startIndex);
    }

    /// <summary>
    /// Parse an array starting at the given index
    /// </summary>
    private static (List<object> array, int nextIndex) ParseArray(string[] lines, int startIndex, int parentIndent)
    {
        var array = new List<object>();
        int i = startIndex;
        int arrayIndent = -1;

        while (i < lines.Length)
        {
            var line = lines[i];
            
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            var lineIndent = GetIndentation(line);
            
            // Set array indent from first item
            if (arrayIndent == -1)
            {
                arrayIndent = lineIndent;
            }

            // Check if we're still in the array
            if (lineIndent < arrayIndent)
            {
                break;
            }

            var trimmed = line.TrimStart();
            
            if (trimmed.StartsWith("-"))
            {
                var itemValue = trimmed.Substring(1).Trim();
                
                // Check if item has inline key:value pairs (object in array)
                if (itemValue.Contains(":"))
                {
                    // Parse as inline object or check for nested object
                    var colonIdx = itemValue.IndexOf(':');
                    var valueAfterColon = itemValue.Substring(colonIdx + 1).Trim();
                    
                    if (string.IsNullOrEmpty(valueAfterColon))
                    {
                        // Nested object follows
                        var (obj, nextIdx) = ParseObject(lines, i + 1, lineIndent);
                        if (obj != null)
                        {
                            array.Add(obj);
                            i = nextIdx - 1;
                        }
                    }
                    else
                    {
                        // Inline key:value - parse as single-property object
                        var key = itemValue.Substring(0, colonIdx).Trim();
                        var value = ParseScalarValue(valueAfterColon);
                        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                        dict[key] = value;
                        array.Add(dict);
                    }
                }
                else if (string.IsNullOrEmpty(itemValue))
                {
                    // Empty item, nested content follows
                    var (nestedValue, nextIdx) = ParseNestedValue(lines, i + 1, lineIndent);
                    if (nestedValue != null)
                    {
                        array.Add(nestedValue);
                        i = nextIdx - 1;
                    }
                }
                else
                {
                    // Simple scalar value
                    array.Add(ParseScalarValue(itemValue));
                }
            }
            else if (lineIndent > arrayIndent && array.Count > 0)
            {
                // Continuation of previous array item (nested properties)
                // This handles multi-line object items in arrays
                // Back up and reparse as object
                i--;
                var (obj, nextIdx) = ParseObject(lines, i + 1, arrayIndent);
                if (obj != null)
                {
                    // Replace the last item with the fully parsed object
                    if (array.Count > 0 && array[array.Count - 1] is Dictionary<string, object> lastDict)
                    {
                        if (obj is Dictionary<string, object> newDict)
                        {
                            foreach (var kvp in newDict)
                            {
                                lastDict[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    i = nextIdx - 1;
                }
            }
            else
            {
                // Not part of array anymore
                break;
            }
            
            i++;
        }

        return (array, i);
    }

    /// <summary>
    /// Parse an object (dictionary) starting at the given index
    /// </summary>
    private static (Dictionary<string, object>? obj, int nextIndex) ParseObject(string[] lines, int startIndex, int parentIndent)
    {
        var obj = new Dictionary<string, object>(StringComparer.Ordinal);
        int i = startIndex;
        int objectIndent = -1;

        while (i < lines.Length)
        {
            var line = lines[i];
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                i++;
                continue;
            }

            var lineIndent = GetIndentation(line);
            
            // Set object indent from first property
            if (objectIndent == -1)
            {
                objectIndent = lineIndent;
            }

            // Check if we're still in the object
            if (lineIndent < objectIndent)
            {
                break;
            }

            // Skip lines with wrong indentation
            if (lineIndent != objectIndent)
            {
                i++;
                continue;
            }

            var trimmed = line.TrimStart();
            
            // Must have a colon
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx == -1)
            {
                i++;
                continue;
            }

            var key = trimmed.Substring(0, colonIdx).Trim();
            var valueText = trimmed.Substring(colonIdx + 1).Trim();

            if (string.IsNullOrEmpty(valueText) || valueText.StartsWith("|") || valueText.StartsWith(">"))
            {
                // Nested value or multi-line string
                if (valueText.StartsWith("|") || valueText.StartsWith(">"))
                {
                    var (multiLineValue, nextIdx) = ParseMultiLineString(lines, i + 1, lineIndent);
                    obj[key] = multiLineValue;
                    i = nextIdx - 1;
                }
                else
                {
                    var (nestedValue, nextIdx) = ParseNestedValue(lines, i + 1, lineIndent);
                    if (nestedValue != null)
                    {
                        obj[key] = nestedValue;
                        i = nextIdx - 1;
                    }
                }
            }
            else
            {
                // Scalar value
                obj[key] = ParseScalarValue(valueText);
            }
            
            i++;
        }

        return obj.Count > 0 ? (obj, i) : (null, i);
    }

    /// <summary>
    /// Parse a multi-line string (literal | or folded >)
    /// </summary>
    private static (string value, int nextIndex) ParseMultiLineString(string[] lines, int startIndex, int parentIndent)
    {
        var sb = new StringBuilder();
        int i = startIndex;

        while (i < lines.Length)
        {
            var line = lines[i];
            
            // Empty lines are included in multi-line strings
            if (string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
                i++;
                continue;
            }

            var lineIndent = GetIndentation(line);
            
            // Check if we're still in the multi-line string
            if (lineIndent <= parentIndent)
            {
                break;
            }

            // Add the line content (with original indentation relative to the string block)
            var content = line.Substring(Math.Min(lineIndent, line.Length));
            sb.AppendLine(content);
            i++;
        }

        return (sb.ToString().TrimEnd(), i);
    }

    /// <summary>
    /// Parse a scalar value (string, number, boolean, null)
    /// </summary>
    private static object ParseScalarValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        value = value.Trim();

        // Remove quotes if present
        if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
            (value.StartsWith("'") && value.EndsWith("'")))
        {
            return value.Substring(1, value.Length - 2);
        }

        // Try to parse as number
        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }
        if (double.TryParse(value, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        // Try to parse as boolean
        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        // Try to parse as null
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase) || 
            value.Equals("~", StringComparison.Ordinal))
        {
            return null!;
        }

        // Return as string
        return value;
    }

    /// <summary>
    /// Get string value from properties dictionary.
    /// </summary>
    private static string? GetStringValue(Dictionary<string, object> properties, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.TryGetValue(key, out var value))
            {
                // If it's a string, return it
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Get array value from properties dictionary.
    /// </summary>
    private static List<string>? GetArrayValue(Dictionary<string, object> properties, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.TryGetValue(key, out var value))
            {
                // If it's a List<object>, convert to List<string>
                if (value is List<object> listObj && listObj.Count > 0)
                {
                    return listObj.Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
                
                // If it's already a List<string>, return it
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
        }
        
        return null;
    }

    /// <summary>
    /// Parse parameters from properties dictionary.
    /// Supports two formats:
    /// 1. Array of objects: parameters: [{name: x, type: y}, ...]
    /// 2. Simple key-value: parameters: {pageSize: 10, enabled: false}
    /// </summary>
    private static List<ParameterDefinition>? ParseParameters(Dictionary<string, object> properties)
    {
        // Try both "parameters" and "$parameters" keys
        if (!properties.TryGetValue("parameters", out var value) && 
            !properties.TryGetValue("$parameters", out value))
        {
            return null;
        }

        var parameters = new List<ParameterDefinition>();

        // Handle array of parameter objects (old format)
        if (value is List<object> list)
        {
            foreach (var item in list)
            {
                if (item is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("name", out var nameObj) && 
                        dict.TryGetValue("type", out var typeObj))
                    {
                        parameters.Add(new ParameterDefinition
                        {
                            Name = nameObj?.ToString() ?? "",
                            Type = typeObj?.ToString() ?? ""
                        });
                    }
                }
            }
        }
        // Handle simple key-value format (new format from problem statement)
        else if (value is Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                var paramName = ToPascalCase(kvp.Key);
                var paramType = InferTypeFromValue(kvp.Value);
                var defaultValue = FormatDefaultValue(kvp.Value);
                
                parameters.Add(new ParameterDefinition
                {
                    Name = paramName,
                    Type = paramType,
                    DefaultValue = defaultValue
                });
            }
        }

        return parameters.Count > 0 ? parameters : null;
    }

    /// <summary>
    /// Convert camelCase or snake_case to PascalCase
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder();
        bool capitalizeNext = true;

        foreach (char c in input)
        {
            if (c == '_' || c == '-')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpper(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Infer C# type from value
    /// </summary>
    private static string InferTypeFromValue(object? value)
    {
        if (value == null)
        {
            return "string?";
        }

        return value switch
        {
            bool _ => "bool",
            int _ => "int",
            long _ => "long",
            double _ => "double",
            float _ => "float",
            string _ => "string",
            _ => "object"
        };
    }

    /// <summary>
    /// Format default value for code generation
    /// </summary>
    private static string? FormatDefaultValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return value switch
        {
            bool b => b ? "true" : "false",
            string s => $"\"{s}\"",
            _ => value.ToString()
        };
    }
}
