using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Markdn.SourceGenerators.Emitters;

/// <summary>
/// Emits RenderTreeBuilder code for Blazor component rendering.
/// Handles BuildRenderTree method generation with markup content, expressions, and components.
/// </summary>
public static class RenderTreeBuilderEmitter
{
    /// <summary>
    /// Generate BuildRenderTree method body with proper handling of expressions and components.
    /// </summary>
    /// <param name="htmlContent">HTML content with Razor syntax (@expressions, components)</param>
    /// <param name="indentLevel">Indentation level (default: 2 for inside class)</param>
    /// <returns>Complete BuildRenderTree method code</returns>
    public static string EmitBuildRenderTree(string htmlContent, int indentLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);
        
        var sb = new StringBuilder();
        sb.AppendLine($"{indent}protected override void BuildRenderTree(");
        sb.AppendLine($"{innerIndent}Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)");
        sb.AppendLine($"{indent}{{");
        
        // T060-T062: Parse content and emit appropriate builder calls
        var statements = ParseContentAndEmitStatements(htmlContent, innerIndent);
        sb.Append(statements);
        
        sb.AppendLine($"{indent}}}");
        
        return sb.ToString();
    }

    /// <summary>
    /// Parse HTML content with Razor syntax and emit appropriate RenderTreeBuilder statements.
    /// T060: Handle inline expressions (@name, @DateTime.Now)
    /// T061: Handle component tags (<Counter />, <Alert>...</Alert>)
    /// </summary>
    private static string ParseContentAndEmitStatements(string content, string indent)
    {
        var sb = new StringBuilder();
        int sequence = 0;
        
        // Check if content contains any Razor syntax
        bool hasRazorSyntax = content.Contains("@") || content.Contains("<Counter") || ContainsComponentTag(content);
        
        if (!hasRazorSyntax)
        {
            // Simple case: no Razor syntax, emit single AddMarkupContent
            sb.AppendLine($"{indent}builder.AddMarkupContent({sequence}, @\"{EscapeForVerbatimString(content)}\");");
            return sb.ToString();
        }
        
        // Complex case: parse and emit mixed content
        // For now, emit as markup content (T060-T062 full implementation needed)
        // TODO: Implement proper parsing of @expressions and component tags
        sb.AppendLine($"{indent}builder.AddMarkupContent({sequence}, @\"{EscapeForVerbatimString(content)}\");");
        
        return sb.ToString();
    }

    /// <summary>
    /// Check if content contains component tags (uppercase first letter).
    /// </summary>
    private static bool ContainsComponentTag(string content)
    {
        // Simple heuristic: look for <UppercaseLetter
        return Regex.IsMatch(content, @"<[A-Z][a-zA-Z0-9]*");
    }

    /// <summary>
    /// Escape content for use in C# verbatim string literal (@"...").
    /// </summary>
    private static string EscapeForVerbatimString(string content)
    {
        // In verbatim strings, double quotes are escaped by doubling them
        return content.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Generate AddMarkupContent statement only (for inline use).
    /// </summary>
    /// <param name="htmlContent">HTML content to render</param>
    /// <param name="sequenceNumber">Sequence number for the render tree</param>
    /// <returns>Single AddMarkupContent statement</returns>
    public static string EmitAddMarkupContent(string htmlContent, int sequenceNumber = 0)
    {
        return $"builder.AddMarkupContent({sequenceNumber}, @\"{EscapeForVerbatimString(htmlContent)}\");";
    }
}
