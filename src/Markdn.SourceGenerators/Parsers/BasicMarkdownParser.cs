using System.Text;
using System.Text.RegularExpressions;

namespace Markdn.SourceGenerators.Parsers;

/// <summary>
/// Basic Markdown parser supporting CommonMark subset without external dependencies.
/// Handles: headings, paragraphs, emphasis, strong, code blocks, inline code, lists, links.
/// </summary>
internal static class BasicMarkdownParser
{
    public static string ConvertToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        var html = new StringBuilder();
        var inCodeBlock = false;
        var codeBlockLanguage = string.Empty;
        var codeBlockContent = new StringBuilder();
        var inList = false;
        var listItems = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Handle code blocks
            if (line.TrimStart().StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    // Start code block
                    inCodeBlock = true;
                    codeBlockLanguage = line.TrimStart().Substring(3).Trim();
                    codeBlockContent.Clear();
                }
                else
                {
                    // End code block
                    var langAttr = !string.IsNullOrEmpty(codeBlockLanguage) 
                        ? $" class=\"language-{EscapeHtml(codeBlockLanguage)}\"" 
                        : string.Empty;
                    html.Append($"<pre><code{langAttr}>{EscapeHtml(codeBlockContent.ToString())}</code></pre>\n");
                    inCodeBlock = false;
                    codeBlockLanguage = string.Empty;
                    codeBlockContent.Clear();
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockContent.AppendLine(line);
                continue;
            }

            // Handle lists
            var listMatch = Regex.Match(line, @"^(\s*)[-*+]\s+(.+)$");
            if (listMatch.Success)
            {
                if (!inList)
                {
                    html.Append("<ul>\n");
                    inList = true;
                }
                var itemContent = ProcessInlineMarkdown(listMatch.Groups[2].Value);
                listItems.Append($"<li>{itemContent}</li>\n");
                continue;
            }
            else if (inList)
            {
                html.Append(listItems.ToString());
                html.Append("</ul>\n");
                inList = false;
                listItems.Clear();
            }

            // Handle headings
            var headingMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Length;
                var content = ProcessInlineMarkdown(headingMatch.Groups[2].Value);
                html.Append($"<h{level}>{content}</h{level}>\n");
                continue;
            }

            // Handle empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Handle paragraphs
            var paragraph = ProcessInlineMarkdown(line);
            html.Append($"<p>{paragraph}</p>\n");
        }

        // Close any open lists
        if (inList)
        {
            html.Append(listItems.ToString());
            html.Append("</ul>\n");
        }

        return html.ToString();
    }

    private static string ProcessInlineMarkdown(string text)
    {
        // Order matters: process in order of precedence

        // Links: [text](url)
        text = Regex.Replace(text, @"\[([^\]]+)\]\(([^\)]+)\)", 
            m => $"<a href=\"{EscapeHtml(m.Groups[2].Value)}\">{EscapeHtml(m.Groups[1].Value)}</a>");

        // Inline code: `code`
        text = Regex.Replace(text, @"`([^`]+)`", 
            m => $"<code>{EscapeHtml(m.Groups[1].Value)}</code>");

        // Bold: **text** or __text__
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        text = Regex.Replace(text, @"__(.+?)__", "<strong>$1</strong>");

        // Italic: *text* or _text_
        text = Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
        text = Regex.Replace(text, @"_(.+?)_", "<em>$1</em>");

        // Strikethrough: ~~text~~
        text = Regex.Replace(text, @"~~(.+?)~~", "<del>$1</del>");

        return text;
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
