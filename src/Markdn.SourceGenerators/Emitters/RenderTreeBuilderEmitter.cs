using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
    // componentNamespace: the namespace of the generated component (used as a fallback)
    public static string EmitBuildRenderTree(string htmlContent, ComponentMetadata? metadata = null, Dictionary<string, string>? componentTypeMap = null, string componentNamespace = "", IEnumerable<string>? availableNamespaces = null, int indentLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);
        
        var sb = new StringBuilder();
        sb.AppendLine($"{indent}protected override void BuildRenderTree(");
        sb.AppendLine($"{innerIndent}Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)");
        sb.AppendLine($"{indent}{{");
        
        int sequence = 0;
        
        // T088: Emit PageTitle component if Title is specified
        if (metadata?.Title != null)
        {
            sb.AppendLine($"{innerIndent}builder.OpenComponent<Microsoft.AspNetCore.Components.Web.PageTitle>({sequence++});");
            sb.AppendLine($"{innerIndent}builder.AddAttribute({sequence++}, \"ChildContent\", (Microsoft.AspNetCore.Components.RenderFragment)((builder2) => {{");
            sb.AppendLine($"{innerIndent}    builder2.AddContent(0, @\"{EscapeForVerbatimString(metadata.Title)}\");");
            sb.AppendLine($"{innerIndent}}}));");
            sb.AppendLine($"{innerIndent}builder.CloseComponent();");
            sb.AppendLine();
        }
        
    // T060-T062: Parse content and emit appropriate builder calls
    var statements = ParseContentAndEmitStatements(htmlContent, innerIndent, ref sequence, componentTypeMap, componentNamespace, availableNamespaces);
        sb.Append(statements);
        
        sb.AppendLine($"{indent}}}");
        
        return sb.ToString();
    }

    /// <summary>
    /// Parse HTML content with Razor syntax and emit appropriate RenderTreeBuilder statements.
    /// T060: Handle inline expressions (@name, @DateTime.Now)
    /// T061: Handle component tags (<Counter />, <Alert>...</Alert>)
    /// </summary>
    private static string ParseContentAndEmitStatements(string content, string indent, ref int sequence, Dictionary<string, string>? componentTypeMap = null, string componentNamespace = "", IEnumerable<string>? availableNamespaces = null)
    {
        var sb = new StringBuilder();
        
        // Parse content into segments (HTML, expressions, components)
        var segments = ParseContentSegments(content);
        
        foreach (var segment in segments)
        {
            switch (segment.Type)
            {
                case SegmentType.Html:
                    // Static HTML content
                    if (!string.IsNullOrWhiteSpace(segment.Content))
                    {
                        sb.AppendLine($"{indent}builder.AddMarkupContent({sequence++}, @\"{EscapeForVerbatimString(segment.Content)}\");");
                    }
                    break;
                    
                case SegmentType.Expression:
                    // T060: Inline expression like @name or @DateTime.Now
                    sb.AppendLine($"{indent}builder.AddContent({sequence++}, {segment.Content});");
                    break;
                    
                    case SegmentType.Component:
                        // T061: Component reference like <Counter />
                        EmitComponentCall(sb, segment, ref sequence, indent, componentTypeMap, componentNamespace, availableNamespaces);
                        break;
                    break;
            }
        }
        
        // If no segments, emit empty markup
        if (segments.Count == 0)
        {
            sb.AppendLine($"{indent}builder.AddMarkupContent({sequence}, @\"{EscapeForVerbatimString(content)}\");");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Parse content into segments of different types.
    /// </summary>
    private static List<ContentSegment> ParseContentSegments(string content)
    {
        var segments = new List<ContentSegment>();
        int position = 0;
        
        while (position < content.Length)
        {
            // Look for @ expression
            var atIndex = content.IndexOf('@', position);
            // Look for component tag
            var componentIndex = FindNextComponentTag(content, position);
            
            // Determine which comes first
            int nextSpecialIndex = -1;
            bool isExpression = false;
            bool isComponent = false;
            
            if (atIndex != -1 && (componentIndex == -1 || atIndex < componentIndex))
            {
                nextSpecialIndex = atIndex;
                isExpression = true;
            }
            else if (componentIndex != -1)
            {
                nextSpecialIndex = componentIndex;
                isComponent = true;
            }
            
            if (nextSpecialIndex == -1)
            {
                // No more special syntax, rest is HTML
                var remaining = content.Substring(position);
                if (!string.IsNullOrEmpty(remaining))
                {
                    segments.Add(new ContentSegment { Type = SegmentType.Html, Content = remaining });
                }
                break;
            }
            
            // Add HTML before special syntax
            if (nextSpecialIndex > position)
            {
                var htmlContent = content.Substring(position, nextSpecialIndex - position);
                segments.Add(new ContentSegment { Type = SegmentType.Html, Content = htmlContent });
            }
            
            // Parse the special syntax
            if (isExpression)
            {
                var (expression, length) = ExtractExpression(content, atIndex);
                segments.Add(new ContentSegment { Type = SegmentType.Expression, Content = expression });
                position = atIndex + length;
            }
            else if (isComponent)
            {
                var (component, length) = ExtractComponent(content, componentIndex);
                if (component != null)
                {
                    segments.Add(component);
                    position = componentIndex + length;
                }
                else
                {
                    // Failed to parse as component, treat as HTML
                    segments.Add(new ContentSegment { Type = SegmentType.Html, Content = content.Substring(componentIndex, 1) });
                    position = componentIndex + 1;
                }
            }
        }
        
        return segments;
    }

    /// <summary>
    /// Extract @ expression from content.
    /// Returns (expression, length).
    /// </summary>
    private static (string expression, int length) ExtractExpression(string content, int startIndex)
    {
        int pos = startIndex + 1; // Skip @
        
        // Check for parenthesized expression: @(...)
        if (pos < content.Length && content[pos] == '(')
        {
            int closeIndex = FindMatchingParen(content, pos);
            if (closeIndex != -1)
            {
                var expr = content.Substring(pos + 1, closeIndex - pos - 1);
                return (expr, closeIndex - startIndex + 1);
            }
        }
        
        // Simple identifier or property chain: @name or @Model.Property or @DateTime.Now.ToString("format")
        var identifierStart = pos;
        while (pos < content.Length)
        {
            char c = content[pos];
            
            // Allow: letters, digits, underscore, dot
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
            {
                pos++;
                continue;
            }
            
            // Allow method calls with parentheses
            if (c == '(')
            {
                var closeIndex = FindMatchingParen(content, pos);
                if (closeIndex != -1)
                {
                    pos = closeIndex + 1;
                    continue;
                }
            }
            
            // End of expression
            break;
        }
        
        if (pos > identifierStart)
        {
            var expr = content.Substring(identifierStart, pos - identifierStart);
            return (expr, pos - startIndex);
        }
        
        // Not a valid expression, treat as literal @
        return ("\"@\"", 1);
    }

    /// <summary>
    /// Find the matching closing parenthesis.
    /// </summary>
    private static int FindMatchingParen(string content, int openIndex)
    {
        int depth = 1;
        int pos = openIndex + 1;
        bool inString = false;
        
        while (pos < content.Length && depth > 0)
        {
            char c = content[pos];
            
            // Handle string literals
            if (c == '"' && (pos == 0 || content[pos - 1] != '\\'))
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return pos;
                    }
                }
            }
            
            pos++;
        }
        
        return -1; // No matching paren
    }

    /// <summary>
    /// Find the next component tag starting from position.
    /// Returns -1 if none found.
    /// </summary>
    private static int FindNextComponentTag(string content, int startPosition)
    {
        // Look for <UppercaseLetter
        for (int i = startPosition; i < content.Length - 1; i++)
        {
            if (content[i] == '<' && i + 1 < content.Length && char.IsUpper(content[i + 1]))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Extract component tag from content.
    /// Returns (segment, length) or (null, 0) if not a valid component.
    /// </summary>
    private static (ContentSegment? segment, int length) ExtractComponent(string content, int startIndex)
    {
        // Parse component tag: <ComponentName attr="value" /> or <ComponentName>...</ComponentName>
        int pos = startIndex + 1; // Skip <
        
        // Extract component name
        var nameStart = pos;
        while (pos < content.Length && (char.IsLetterOrDigit(content[pos]) || content[pos] == '_'))
        {
            pos++;
        }
        
        if (pos == nameStart)
        {
            return (null, 0); // No valid name
        }
        
        var componentName = content.Substring(nameStart, pos - nameStart);
        
        // Skip whitespace
        while (pos < content.Length && char.IsWhiteSpace(content[pos]))
        {
            pos++;
        }
        
        // Parse attributes (simplified - T057)
        var parameters = new List<(string name, string value)>();
        while (pos < content.Length && content[pos] != '>' && content[pos] != '/')
        {
            // Parse attribute name
            var attrStart = pos;
            while (pos < content.Length && content[pos] != '=' && content[pos] != '>' && !char.IsWhiteSpace(content[pos]))
            {
                pos++;
            }
            
            if (pos == attrStart)
            {
                break;
            }
            
            var attrName = content.Substring(attrStart, pos - attrStart);
            
            // Skip whitespace and =
            while (pos < content.Length && (char.IsWhiteSpace(content[pos]) || content[pos] == '='))
            {
                pos++;
            }
            
            // Parse attribute value
            string attrValue = "";
            if (pos < content.Length && content[pos] == '"')
            {
                pos++; // Skip opening "
                var valueStart = pos;
                while (pos < content.Length && content[pos] != '"')
                {
                    pos++;
                }
                attrValue = content.Substring(valueStart, pos - valueStart);
                if (pos < content.Length)
                {
                    pos++; // Skip closing "
                }
            }
            
            parameters.Add((attrName, attrValue));
            
            // Skip whitespace
            while (pos < content.Length && char.IsWhiteSpace(content[pos]))
            {
                pos++;
            }
        }
        
        // Check for self-closing tag />
        bool isSelfClosing = false;
        string? childContent = null;
        
        if (pos < content.Length && content[pos] == '/')
        {
            pos++; // Skip /
            if (pos < content.Length && content[pos] == '>')
            {
                pos++; // Skip >
                isSelfClosing = true;
            }
        }
        else if (pos < content.Length && content[pos] == '>')
        {
            pos++; // Skip >
            
            // Look for closing tag
            var closeTag = $"</{componentName}>";
            var closeIndex = content.IndexOf(closeTag, pos);
            
            if (closeIndex != -1)
            {
                childContent = content.Substring(pos, closeIndex - pos);
                pos = closeIndex + closeTag.Length;
            }
            else
            {
                // No closing tag, treat as self-closing
                isSelfClosing = true;
            }
        }
        
        var segment = new ContentSegment
        {
            Type = SegmentType.Component,
            ComponentName = componentName,
            Parameters = parameters,
            ChildContent = childContent
        };
        
        return (segment, pos - startIndex);
    }

    /// <summary>
    /// Emit RenderTreeBuilder calls for a component.
    /// T061: OpenComponent, AddAttribute, CloseComponent
    /// T062: ChildContent with RenderFragment
    /// </summary>
    private static void EmitComponentCall(StringBuilder sb, ContentSegment segment, ref int sequence, string indent, Dictionary<string, string>? componentTypeMap = null, string componentNamespace = "", IEnumerable<string>? availableNamespaces = null)
    {
        // Determine fully-qualified component type using provided map or fallback to componentNamespace
        string qualifiedType;
        if (availableNamespaces != null)
        {
            // If the generator emitted using directives for candidate namespaces, prefer
            // leaving the type unqualified so the using directives can resolve it.
            qualifiedType = segment.ComponentName;
        }
        else if (componentTypeMap != null && componentTypeMap.TryGetValue(segment.ComponentName, out var resolvedNamespace) && !string.IsNullOrEmpty(resolvedNamespace))
        {
            // If we resolved to a specific namespace for this component, use fully-qualified name
            qualifiedType = $"global::{resolvedNamespace}.{segment.ComponentName}";
        }
        else if (!string.IsNullOrEmpty(componentNamespace))
        {
            // Fallback to the generated component's namespace
            qualifiedType = $"global::{componentNamespace}.{segment.ComponentName}";
        }
        else
        {
            qualifiedType = segment.ComponentName; // best-effort fallback (may fail to compile)
        }
        sb.AppendLine($"{indent}builder.OpenComponent({sequence++}, typeof({qualifiedType}));");
        
        // Add attributes/parameters
        if (segment.Parameters != null)
        {
            foreach (var (name, value) in segment.Parameters)
            {
                // Check if value is an expression (starts with @)
                if (value.StartsWith("@"))
                {
                    var expr = value.Substring(1);
                    sb.AppendLine($"{indent}builder.AddAttribute({sequence++}, \"{name}\", {expr});");
                }
                else
                {
                    sb.AppendLine($"{indent}builder.AddAttribute({sequence++}, \"{name}\", \"{value}\");");
                }
            }
        }
        
        // T062: Add child content if present
        if (!string.IsNullOrEmpty(segment.ChildContent))
        {
            sb.AppendLine($"{indent}builder.AddAttribute({sequence++}, \"ChildContent\", (Microsoft.AspNetCore.Components.RenderFragment)((builder2) => {{");
            sb.AppendLine($"{indent}    builder2.AddMarkupContent(0, @\"{EscapeForVerbatimString(segment.ChildContent)}\");");
            sb.AppendLine($"{indent}}}));");
        }
        
        // CloseComponent
        sb.AppendLine($"{indent}builder.CloseComponent();");
    }

    /// <summary>
    /// Content segment types for parsing.
    /// </summary>
    private enum SegmentType
    {
        Html,
        Expression,
        Component
    }

    /// <summary>
    /// Represents a parsed content segment.
    /// </summary>
    private class ContentSegment
    {
        public SegmentType Type { get; set; }
        public string Content { get; set; } = "";
        public string? ComponentName { get; set; }
        public List<(string name, string value)>? Parameters { get; set; }
        public string? ChildContent { get; set; }
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
