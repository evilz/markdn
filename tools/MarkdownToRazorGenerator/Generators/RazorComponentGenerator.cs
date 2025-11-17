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
        sb.AppendLine("@using Microsoft.AspNetCore.Components");
        sb.AppendLine("@using Microsoft.AspNetCore.Components.Web");
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
}
