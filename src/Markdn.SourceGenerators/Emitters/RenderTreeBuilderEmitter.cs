using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Markdn.SourceGenerators.Models;

namespace Markdn.SourceGenerators.Emitters;

/// <summary>
/// Emits RenderTreeBuilder code for Blazor component rendering.
/// Handles BuildRenderTree method generation with markup content, expressions, and components.
/// </summary>
public static class RenderTreeBuilderEmitter
{
    /// <summary>
    /// Generate BuildRenderTree method body with proper handling of expressions, components, and PageTitle.
    /// </summary>
    /// <param name="htmlContent">HTML content with Razor syntax (@expressions, components)</param>
    /// <param name="metadata">Component metadata (for PageTitle generation)</param>
    /// <param name="indentLevel">Indentation level (default: 2 for inside class)</param>
    /// <returns>Complete BuildRenderTree method code</returns>
    // componentTypeMap: optional map of component simple name -> fully-qualified namespace (from compilation)
    public static string EmitBuildRenderTree(string htmlContent, ComponentMetadata? metadata = null, Dictionary<string, string>? componentTypeMap = null, int indentLevel = 2, bool assumeComponentsResolvable = false)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        var sb = new StringBuilder();
        sb.AppendLine($"{indent}protected override void BuildRenderTree(");
        sb.AppendLine($"{innerIndent}Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)");
        sb.AppendLine($"{indent}{{");

        var seq = new SequenceCounter();

        // If Title specified, emit PageTitle component
        if (metadata?.Title != null)
        {
            sb.AppendLine($"{innerIndent}builder.OpenComponent<Microsoft.AspNetCore.Components.Web.PageTitle>({seq.Next()});");
            sb.AppendLine($"{innerIndent}builder.AddAttribute({seq.Next()}, \"ChildContent\", (Microsoft.AspNetCore.Components.RenderFragment)((builder2) => {{");
            sb.AppendLine($"{innerIndent}    builder2.AddContent(0, @\"{EscapeForVerbatimString(metadata.Title)}\");");
            sb.AppendLine($"{innerIndent}}}));");
            sb.AppendLine($"{innerIndent}builder.CloseComponent();");
            sb.AppendLine();
        }

        // Emit full markup content as-is (preserves Razor: @page, @if, @Name, component tags, etc.)
        if (!string.IsNullOrEmpty(htmlContent))
        {
            sb.AppendLine($"{innerIndent}builder.AddMarkupContent({seq.Next()}, @\"{EscapeForVerbatimString(htmlContent)}\");");
        }

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
    /// Simple sequence counter for deterministic sequence numbers in RenderTreeBuilder emissions.
    /// Encapsulates an integer counter and exposes Next() for allocation.
    /// </summary>
    private struct SequenceCounter
    {
        private int _value;

        public int Next()
        {
            return _value++;
        }

        public int Peek() => _value;

        public void Reset(int value = 0) => _value = value;
    }
}
