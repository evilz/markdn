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

        // Output the HTML content directly (no wrapper)
        sb.AppendLine(htmlContent);
        
        // Add @code block if there are variables or parameters
        if ((metadata.Variables != null && metadata.Variables.Count > 0) || 
            (metadata.Parameters != null && metadata.Parameters.Count > 0))
        {
            sb.AppendLine();
            sb.AppendLine("@code {");
            
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
                    var varType = InferType(variable.Value);
                    var varValue = FormatValue(variable.Value);
                    sb.AppendLine($"    private {varType} {variable.Key} = {varValue};");
                }
            }
            
            sb.AppendLine("}");
        }

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
            _ => value.GetType().Name
        };
    }
    
    /// <summary>
    /// Formats value for C# code
    /// </summary>
    private string FormatValue(object? value)
    {
        if (value == null) return "null";
        
        return value switch
        {
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b.ToString().ToLower(),
            DateTime dt => $"DateTime.Parse(\"{dt:O}\")",
            _ => value.ToString() ?? "null"
        };
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
