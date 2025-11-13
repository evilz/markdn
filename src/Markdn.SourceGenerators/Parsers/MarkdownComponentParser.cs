using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdn.SourceGenerators.Models;

namespace Markdn.SourceGenerators.Parsers;

/// <summary>
/// Orchestrates parsing of a Markdown file into a MarkdownComponentModel.
/// Steps:
///  - Extract YAML front matter -> ComponentMetadata
///  - Preserve Razor syntax (@code, @expressions, component tags)
///  - Convert Markdown body to HTML (BasicMarkdownParser)
///  - Restore preserved Razor syntax into HTML where appropriate
///  - Extract @code blocks into CodeBlock entities
/// </summary>
internal static class MarkdownComponentParser
{
    public static MarkdownComponentModel Parse(string sourceFilePath, string content, string defaultNamespace)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        // 1. Extract YAML front matter
        var (metadata, markdownBody, yamlErrors) = YamlFrontMatterParser.Parse(content);

        // 2. Preserve razor syntax (placeholders)
        var preserver = new RazorPreserver();
        var preserved = preserver.ExtractRazorSyntax(markdownBody);

        // 3. Convert Markdown to HTML using basic parser (MVP)
        var html = BasicMarkdownParser.ConvertToHtml(preserved);

        // 4. Restore Razor syntax into HTML (except @code blocks which we emit separately)
        var restoredHtml = preserver.RestoreRazorSyntax(html, excludeCodeBlocks: true);

        // 5. Extract code blocks from preserved blocks
        var codeBlocks = new List<CodeBlock>();
        foreach (var kv in preserver.GetPreservedBlocks())
        {
            var original = kv.Value.TrimStart();
            if (original.StartsWith("@code", StringComparison.Ordinal))
            {
                // Strip leading @code and the outer braces if present
                var contentStart = original.IndexOf('{');
                var contentText = original;
                if (contentStart >= 0)
                {
                    var close = MarkdownComponentParserHelpers.FindMatchingBraceIndex(original, contentStart);
                    if (close > contentStart)
                    {
                        contentText = original.Substring(contentStart + 1, close - contentStart - 1).Trim();
                    }
                }

                codeBlocks.Add(new CodeBlock { Content = contentText, Location = null });
            }
        }

        // 6. Build a single HtmlSegment with the restored HTML (split into segments can be added later)
        var segments = new List<HtmlSegment>
        {
            new HtmlSegment { Type = SegmentType.StaticHtml, Content = restoredHtml }
        };

        var mdContent = new MarkdownContent { Segments = segments };

        // 7. Compute component name from filename
        var fileName = Path.GetFileName(sourceFilePath) ?? sourceFilePath;
        var componentName = GenerateComponentName(fileName);

        var model = new MarkdownComponentModel
        {
            FileName = fileName,
            ComponentName = componentName,
            Namespace = !string.IsNullOrWhiteSpace(metadata.Namespace) ? metadata.Namespace! : defaultNamespace,
            Metadata = metadata,
            Content = mdContent,
            CodeBlocks = codeBlocks,
            SourceFilePath = sourceFilePath
        };

        return model;
    }

    private static string GenerateComponentName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Component";
        }

        var name = Path.GetFileNameWithoutExtension(fileName);

        // Remove leading date prefix like 2024-11-10-, 20241110-, etc.
        // Look for pattern yyyy-mm-dd- or yyyyMMdd- at start
        if (name.Length >= 8)
        {
            // yyyy-mm-dd-
            if (char.IsDigit(name[0]) && name.Contains('-'))
            {
                // remove until last part after the date dashes
                var parts = name.Split('-');
                if (parts.Length >= 4 && parts[0].Length == 4 && parts[1].Length == 2 && parts[2].Length == 2)
                {
                    name = parts[parts.Length - 1];
                }
            }
            else if (name.Length >= 8 && int.TryParse(name.Substring(0, 8), out _))
            {
                // yyyyMMdd prefix
                name = name.Substring(8).TrimStart('-');
            }
        }

        // Convert kebab-case or snake_case to PascalCase
        var parts2 = name.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var pascal = string.Concat(parts2.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));

        // If result is empty, fallback
        if (string.IsNullOrWhiteSpace(pascal))
        {
            pascal = "Component";
        }

        // Ensure it's a valid identifier (simple check)
        if (!char.IsLetter(pascal[0]) && pascal.Length > 1)
        {
            pascal = "C" + pascal;
        }

        return pascal;
    }
}

internal static class MarkdownComponentParserHelpers
{
    public static int FindMatchingBraceIndex(string text, int openIndex)
    {
        int depth = 1;
        for (int i = openIndex + 1; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }
        return -1;
    }
}
