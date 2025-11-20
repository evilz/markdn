using System.Text;
using MarkdownToRazorGenerator.Models;

namespace MarkdownToRazorGenerator.Generators;

/// <summary>
/// Generates Razor component files from Markdown content
/// </summary>
public class RazorComponentGenerator
{
    /// <summary>
    /// Generates a Razor component file content
    /// </summary>
    public string Generate(MarkdownMetadata metadata, string htmlContent, string route, string title)
    {
        return Generate(metadata, htmlContent, route, title, new List<Parsers.SectionInfo>());
    }

    /// <summary>
    /// Generates a Razor component file content with section support
    /// </summary>
    public string Generate(MarkdownMetadata metadata, string htmlContent, string route, string title, List<Parsers.SectionInfo> sections)
    {
        var sb = new StringBuilder();

        // Add @page directive
        sb.AppendLine($"@page \"{route}\"");
        
        // Add standard using directives
        sb.AppendLine("@using Microsoft.AspNetCore.Components");
        sb.AppendLine("@using Microsoft.AspNetCore.Components.Web");
        
        // Add component namespace directives from metadata
        if (metadata.ComponentNamespaces != null && metadata.ComponentNamespaces.Count > 0)
        {
            foreach (var ns in metadata.ComponentNamespaces)
            {
                sb.AppendLine($"@using {ns}");
            }
        }
        
        sb.AppendLine();

        // Add @layout directive if specified
        if (!string.IsNullOrWhiteSpace(metadata.Layout))
        {
            sb.AppendLine($"@layout {metadata.Layout}");
            sb.AppendLine();
        }

        // Add PageTitle
        sb.AppendLine($"<PageTitle>{EscapeForRazor(title)}</PageTitle>");
        sb.AppendLine();
        
        // Wrap content in CascadingValue to pass metadata to layout
        sb.AppendLine("<CascadingValue Value=\"@_pageMetadata\">");
        sb.AppendLine();

        // Add SectionContent components for each section
        if (sections != null && sections.Count > 0)
        {
            foreach (var section in sections)
            {
                sb.AppendLine($"<SectionContent SectionName=\"{section.Name}\">");
                sb.AppendLine(section.Content);
                sb.AppendLine("</SectionContent>");
                sb.AppendLine();
            }
        }

        // Output the HTML content directly (no wrapper)
        sb.AppendLine(htmlContent);
        
        // Close CascadingValue
        sb.AppendLine();
        sb.AppendLine("</CascadingValue>");
        
        // Add @code block - always needed for _pageMetadata
        sb.AppendLine();
        sb.AppendLine("@code {");
        
        // Define PageMetadata class inline for this component
        sb.AppendLine("    private class PageMetadata");
        sb.AppendLine("    {");
        sb.AppendLine("        public string? Title { get; set; }");
        sb.AppendLine("        public string? Slug { get; set; }");
        sb.AppendLine("        public string? Route { get; set; }");
        sb.AppendLine("        public string? Summary { get; set; }");
        sb.AppendLine("        public DateTime? Date { get; set; }");
        sb.AppendLine("        public List<string>? Tags { get; set; }");
        sb.AppendLine("        public Dictionary<string, object>? AdditionalData { get; set; }");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        // Add PageMetadata instance to pass to layout
        sb.AppendLine($"    private readonly PageMetadata _pageMetadata = new()");
        sb.AppendLine("    {");
        if (!string.IsNullOrWhiteSpace(title))
        {
            sb.AppendLine($"        Title = {FormatValue(title)},");
        }
        if (!string.IsNullOrWhiteSpace(metadata.Slug))
        {
            sb.AppendLine($"        Slug = {FormatValue(metadata.Slug)},");
        }
        if (!string.IsNullOrWhiteSpace(route))
        {
            sb.AppendLine($"        Route = {FormatValue(route)},");
        }
        if (!string.IsNullOrWhiteSpace(metadata.Summary))
        {
            sb.AppendLine($"        Summary = {FormatValue(metadata.Summary)},");
        }
        if (metadata.Date.HasValue)
        {
            sb.AppendLine($"        Date = {FormatValue(metadata.Date.Value)},");
        }
        if (metadata.Tags != null && metadata.Tags.Count > 0)
        {
            sb.Append("        Tags = new List<string> { ");
            sb.Append(string.Join(", ", metadata.Tags.Select(t => FormatValue(t))));
            sb.AppendLine(" },");
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        
        // Add parameters
        if (metadata.Parameters != null && metadata.Parameters.Count > 0)
        {
            foreach (var param in metadata.Parameters)
            {
                var paramType = InferType(param.Value);
                var paramValue = FormatValue(param.Value);
                sb.AppendLine($"    [Parameter]");
                sb.AppendLine($"    public {paramType} {CapitalizeFirst(param.Key)} {{ get; set; }} = {paramValue};");
                sb.AppendLine();
            }
        }
        
        // Add variables
        if (metadata.Variables != null && metadata.Variables.Count > 0)
        {
            foreach (var variable in metadata.Variables)
            {
                var varName = SanitizeFieldName(variable.Key);
                var varType = InferType(variable.Value);
                var varValue = FormatValue(variable.Value);
                sb.AppendLine($"    private {varType} {varName} = {varValue};");
            }
        }
        
        // Add helper method for creating ExpandoObject from dictionary
        if (metadata.Variables != null && metadata.Variables.Count > 0)
        {
            bool hasExpandoVars = false;
            foreach (var variable in metadata.Variables)
            {
                if (variable.Value is Dictionary<string, object>)
                {
                    hasExpandoVars = true;
                    break;
                }
                if (variable.Value is List<object> list)
                {
                    foreach (var item in list)
                    {
                        if (item is Dictionary<string, object>)
                        {
                            hasExpandoVars = true;
                            break;
                        }
                    }
                }
            }
            
            if (hasExpandoVars)
            {
                sb.AppendLine();
                sb.AppendLine("    private static dynamic CreateExpando(Dictionary<string, object> dict)");
                sb.AppendLine("    {");
                sb.AppendLine("        var expando = new System.Dynamic.ExpandoObject();");
                sb.AppendLine("        var expandoDict = (IDictionary<string, object>)expando;");
                sb.AppendLine("        foreach (var kvp in dict)");
                sb.AppendLine("        {");
                sb.AppendLine("            expandoDict[kvp.Key] = kvp.Value;");
                sb.AppendLine("        }");
                sb.AppendLine("        return expando;");
                sb.AppendLine("    }");
            }
        }
        
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Escapes text for use in Razor markup
    /// </summary>
    private string EscapeForRazor(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("&", "&amp;");
    }
    
    /// <summary>
    /// Infers C# type from object value
    /// </summary>
    private string InferType(object? value)
    {
        if (value == null) return "object?";
        
        return value switch
        {
            string => "string",
            int => "int",
            long => "long",
            double => "double",
            float => "float",
            bool => "bool",
            DateTime => "DateTime",
            Dictionary<string, object> => "dynamic",
            List<object> => "dynamic",
            _ => value.GetType().Name
        };
    }
    
    /// <summary>
    /// Formats value for C# code
    /// </summary>
    private string FormatValue(object? value)
    {
        if (value == null) return "null";
        
        if (value is Dictionary<string, object> dict)
        {
            return GenerateExpandoInitializer(dict, 1);
        }
        
        if (value is List<object> list)
        {
            return GenerateListInitializer(list, 1);
        }
        
        return value switch
        {
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b.ToString().ToLower(),
            DateTime dt => $"DateTime.Parse(\"{dt:O}\")",
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Generate ExpandoObject initializer from dictionary
    /// </summary>
    private string GenerateExpandoInitializer(Dictionary<string, object> dict, int indentLevel)
    {
        if (dict.Count == 0)
        {
            return "new System.Dynamic.ExpandoObject()";
        }

        var sb = new StringBuilder();
        sb.Append("CreateExpando(new Dictionary<string, object> {");

        bool first = true;
        foreach (var kvp in dict)
        {
            if (!first)
            {
                sb.Append(", ");
            }
            first = false;

            var propName = kvp.Key.Replace("\"", "\\\"");
            var propValue = FormatNestedValue(kvp.Value, indentLevel + 1);
            sb.Append($" {{ \"{propName}\", {propValue} }}");
        }

        sb.Append(" })");
        
        return sb.ToString();
    }

    /// <summary>
    /// Generate list initializer
    /// </summary>
    private string GenerateListInitializer(List<object> list, int indentLevel)
    {
        if (list.Count == 0)
        {
            return "new System.Collections.Generic.List<dynamic>()";
        }

        var sb = new StringBuilder();
        sb.Append("new System.Collections.Generic.List<dynamic> {");

        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            var itemValue = FormatNestedValue(list[i], indentLevel + 1);
            sb.Append($" {itemValue}");
        }

        sb.Append(" }");
        
        return sb.ToString();
    }

    /// <summary>
    /// Format nested value (for use inside ExpandoObject or List initializers)
    /// </summary>
    private string FormatNestedValue(object? value, int indentLevel)
    {
        if (value == null) return "null";
        
        if (value is Dictionary<string, object> dict)
        {
            return GenerateExpandoInitializer(dict, indentLevel);
        }
        
        if (value is List<object> list)
        {
            return GenerateListInitializer(list, indentLevel);
        }
        
        return value switch
        {
            string s => $"@\"{s.Replace("\"", "\"\"")}\"",
            bool b => b.ToString().ToLower(),
            DateTime dt => $"DateTime.Parse(\"{dt:O}\")",
            int i => i.ToString(),
            long l => l.ToString() + "L",
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f",
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Sanitize property name for ExpandoObject (must be valid C# identifier)
    /// </summary>
    private string SanitizePropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Property";
        }

        var sb = new StringBuilder();
        bool capitalizeNext = true;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            if (c == '-' || c == '_' || c == ' ')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                if (sb.Length == 0)
                {
                    // First character should be uppercase for property
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
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
        }

        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "Property" : result;
    }
    
    /// <summary>
    /// Sanitize field name (convert kebab-case to camelCase, handle invalid chars)
    /// </summary>
    private string SanitizeFieldName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "field";
        }

        var sb = new StringBuilder();
        bool capitalizeNext = false;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            if (c == '-' || c == '_' || c == ' ')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                if (sb.Length == 0)
                {
                    // First character should be lowercase
                    sb.Append(char.ToLower(c));
                    capitalizeNext = false;
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
        }

        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "field" : result;
    }
    
    /// <summary>
    /// Capitalizes first letter of a string
    /// </summary>
    private string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
