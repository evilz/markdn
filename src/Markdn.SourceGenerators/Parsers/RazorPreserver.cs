using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Markdn.SourceGenerators.Parsers;

/// <summary>
/// Preserves Razor syntax (@code blocks, @expressions, component tags) through Markdown processing.
/// Extracts Razor syntax before Markdown parsing and restores it after.
/// </summary>
public sealed class RazorPreserver
{
    private readonly Dictionary<string, string> _preservedBlocks = new Dictionary<string, string>();
    private int _blockCounter = 0;

    /// <summary>
    /// Extract Razor syntax from content and replace with placeholders.
    /// </summary>
    /// <param name="content">Content with Razor syntax</param>
    /// <returns>Content with Razor syntax replaced by placeholders</returns>
    public string ExtractRazorSyntax(string content)
    {
        var result = content;

        // 1. Preserve @code blocks first (highest priority)
        result = PreserveCodeBlocks(result);

        // 2. Preserve Razor control flow statements (@if, @foreach, @for, @while, @switch)
        result = PreserveControlFlowStatements(result);

        // 3. Preserve component tags (e.g., <Counter />, <WeatherForecast>...)
        result = PreserveComponentTags(result);

        // 4. Preserve @expressions (e.g., @DateTime.Now, @(expression))
        result = PreserveExpressions(result);

        return result;
    }

    /// <summary>
    /// Restore preserved Razor syntax from placeholders.
    /// </summary>
    /// <param name="content">Content with placeholders</param>
    /// <param name="excludeCodeBlocks">If true, @code blocks won't be restored (they're emitted separately)</param>
    /// <returns>Content with original Razor syntax restored</returns>
    public string RestoreRazorSyntax(string content, bool excludeCodeBlocks = false)
    {
        var result = content;

        foreach (var kvp in _preservedBlocks)
        {
            // Skip @code blocks if requested (they're emitted separately in the class)
            if (excludeCodeBlocks && kvp.Value.StartsWith("@code"))
            {
                // Replace placeholder with empty string to remove it from HTML
                result = result.Replace(kvp.Key, string.Empty);
                continue;
            }
            
            result = result.Replace(kvp.Key, kvp.Value);
        }

        return result;
    }

    /// <summary>
    /// Get the preserved blocks dictionary for code extraction.
    /// </summary>
    /// <returns>Dictionary of placeholder to original content mappings</returns>
    public IReadOnlyDictionary<string, string> GetPreservedBlocks()
    {
        return _preservedBlocks;
    }

    /// <summary>
    /// Preserve @code {} blocks.
    /// Pattern: @code { ... } (can be multiline)
    /// </summary>
    private string PreserveCodeBlocks(string content)
    {
        // Match @code { ... } including nested braces
        var pattern = @"@code\s*\{";
        var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var startIndex = match.Index;
            var openBraceIndex = content.IndexOf('{', startIndex);
            
            if (openBraceIndex == -1) continue;

            // Find matching closing brace
            var closeBraceIndex = FindMatchingBrace(content, openBraceIndex);
            
            if (closeBraceIndex == -1) continue;

            // Extract the entire @code block
            var codeBlock = content.Substring(startIndex, closeBraceIndex - startIndex + 1);
            var placeholder = CreatePlaceholder();
            
            _preservedBlocks[placeholder] = codeBlock;
            content = content.Remove(startIndex, codeBlock.Length).Insert(startIndex, placeholder);
        }

        return content;
    }

    /// <summary>
    /// Preserve Razor control flow statements (@if, @foreach, @for, @while, @switch).
    /// Pattern: @keyword (condition) { ... } with proper brace matching
    /// </summary>
    private string PreserveControlFlowStatements(string content)
    {
        // Match @if, @foreach, @for, @while, @switch followed by condition/header and block
        var keywords = new[] { "if", "foreach", "for", "while", "switch" };
        
        foreach (var keyword in keywords)
        {
            // Pattern: @keyword with optional whitespace and parentheses
            // Match @if (condition) or @foreach (item in items), then find the opening brace
            var pattern = $@"@{keyword}\s*\(";
            
            var matches = Regex.Matches(content, pattern, RegexOptions.Multiline);

            // Process matches in reverse to maintain indices
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                var startIndex = match.Index;
                
                // Find the closing parenthesis
                var parenLevel = 1;
                var searchIndex = match.Index + match.Length;
                var closingParenIndex = -1;
                
                while (searchIndex < content.Length && parenLevel > 0)
                {
                    if (content[searchIndex] == '(') parenLevel++;
                    else if (content[searchIndex] == ')') parenLevel--;
                    
                    if (parenLevel == 0)
                    {
                        closingParenIndex = searchIndex;
                        break;
                    }
                    searchIndex++;
                }
                
                if (closingParenIndex == -1) continue;
                
                // Find the opening brace after the closing parenthesis
                var braceSearchIndex = closingParenIndex + 1;
                var openBraceIndex = -1;
                
                while (braceSearchIndex < content.Length)
                {
                    var ch = content[braceSearchIndex];
                    if (ch == '{')
                    {
                        openBraceIndex = braceSearchIndex;
                        break;
                    }
                    else if (!char.IsWhiteSpace(ch))
                    {
                        // Non-whitespace character before brace - invalid syntax
                        break;
                    }
                    braceSearchIndex++;
                }
                
                if (openBraceIndex == -1) continue;

                // Find matching closing brace
                var closeBraceIndex = FindMatchingBrace(content, openBraceIndex);
                
                if (closeBraceIndex == -1) continue;

                // Extract the entire control flow block
                var controlFlowBlock = content.Substring(startIndex, closeBraceIndex - startIndex + 1);
                var placeholder = CreatePlaceholder();
                
                _preservedBlocks[placeholder] = controlFlowBlock;
                content = content.Remove(startIndex, controlFlowBlock.Length).Insert(startIndex, placeholder);
            }
        }

        return content;
    }

    /// <summary>
    /// Preserve Razor component tags (e.g., <Counter />, <WeatherForecast>...</WeatherForecast>).
    /// </summary>
    private string PreserveComponentTags(string content)
    {
        // Match component tags: <ComponentName ... /> or <ComponentName>...</ComponentName>
        // Component names start with uppercase letter
        var pattern = @"<([A-Z][a-zA-Z0-9_]*)((?:\s+[^>]*)?)(?:\s*/>|>(.*?)</\1>)";
        
        var result = Regex.Replace(content, pattern, match =>
        {
            var componentTag = match.Value;
            var placeholder = CreatePlaceholder();
            _preservedBlocks[placeholder] = componentTag;
            return placeholder;
        }, RegexOptions.Singleline);

        return result;
    }

    /// <summary>
    /// Preserve @expressions (e.g., @DateTime.Now, @(expression), @Model.Property).
    /// </summary>
    private string PreserveExpressions(string content)
    {
        // Match @identifier or @(expression)
        // Pattern 1: @(expression with parentheses)
        var parenPattern = @"@\([^)]+\)";
        content = Regex.Replace(content, parenPattern, match =>
        {
            var placeholder = CreatePlaceholder();
            _preservedBlocks[placeholder] = match.Value;
            return placeholder;
        });

        // Pattern 2: @identifier or @identifier.property.chain
        // Use negative lookahead to exclude Razor keywords (if, foreach, for, while, switch, code, etc.)
        // These keywords are handled separately by control flow or code block preservation
        var identifierPattern = @"@(?!(if|foreach|for|while|switch|code|else|elseif|using|try|catch|finally)\b)([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)";
        
        content = Regex.Replace(content, identifierPattern, match =>
        {
            // Skip if already a placeholder (HTML comment)
            if (match.Value.Contains("<!--RAZOR-PRESERVE-"))
                return match.Value;

            var placeholder = CreatePlaceholder();
            _preservedBlocks[placeholder] = match.Value;
            return placeholder;
        });

        return content;
    }

    /// <summary>
    /// Find the matching closing brace for an opening brace.
    /// Handles nested braces correctly.
    /// </summary>
    private static int FindMatchingBrace(string content, int openBraceIndex)
    {
        int braceCount = 1;
        int index = openBraceIndex + 1;

        while (index < content.Length && braceCount > 0)
        {
            char c = content[index];
            
            if (c == '{')
                braceCount++;
            else if (c == '}')
                braceCount--;

            if (braceCount == 0)
                return index;

            index++;
        }

        return -1; // No matching brace found
    }

    /// <summary>
    /// Create a unique placeholder for preserved content.
    /// Use a pattern that won't be interpreted as Markdown.
    /// </summary>
    private string CreatePlaceholder()
    {
        // Use HTML comment style to avoid Markdown processing
        return $"<!--RAZOR-PRESERVE-{_blockCounter++}-->";
    }
}
