using System.Text;

namespace Markdn.SourceGenerators.Emitters;

/// <summary>
/// Emits RenderTreeBuilder code for Blazor component rendering.
/// Handles BuildRenderTree method generation with markup content.
/// </summary>
public static class RenderTreeBuilderEmitter
{
    /// <summary>
    /// Generate BuildRenderTree method body with AddMarkupContent.
    /// </summary>
    /// <param name="htmlContent">HTML content to render</param>
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
        sb.AppendLine($"{innerIndent}builder.AddMarkupContent(0, @\"{EscapeForVerbatimString(htmlContent)}\");");
        sb.AppendLine($"{indent}}}");
        
        return sb.ToString();
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
