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

        // Add article wrapper with markdown body
        sb.AppendLine("<article class=\"markdown-body\">");
        sb.AppendLine("    @((MarkupString)HtmlContent)");
        sb.AppendLine("</article>");
        sb.AppendLine();

        // Add @code block with HTML content
        sb.AppendLine("@code {");
        sb.AppendLine("    private static readonly string HtmlContent = @\"");
        
        // Escape HTML content for C# string literal
        var escapedHtml = EscapeForCSharpString(htmlContent);
        sb.AppendLine(escapedHtml);
        
        sb.AppendLine("\";");
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
    /// Escapes HTML content for use in C# verbatim string literal
    /// </summary>
    private string EscapeForCSharpString(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        // In verbatim strings (@""), we only need to escape double quotes by doubling them
        return html.Replace("\"", "\"\"");
    }
}
