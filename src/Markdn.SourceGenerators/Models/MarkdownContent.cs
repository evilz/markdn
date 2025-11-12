using System;
using System.Collections.Generic;

namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Parsed Markdown body content with segments.
/// </summary>
public sealed class MarkdownContent
{
    /// <summary>
    /// Content segments (HTML, expressions, component references)
    /// </summary>
    public required IReadOnlyList<HtmlSegment> Segments { get; init; }

    /// <summary>
    /// Empty content instance
    /// </summary>
    public static MarkdownContent Empty { get; } = new() { Segments = Array.Empty<HtmlSegment>() };
}
