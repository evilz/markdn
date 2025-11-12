namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Type of HTML/content segment.
/// </summary>
public enum SegmentType
{
    /// <summary>
    /// Static HTML content (from Markdown)
    /// </summary>
    StaticHtml,

    /// <summary>
    /// Inline Razor expression (e.g., @DateTime.Now, @UserName)
    /// </summary>
    RazorExpression,

    /// <summary>
    /// Blazor component reference (e.g., &lt;Counter /&gt;)
    /// </summary>
    ComponentReference
}

/// <summary>
/// Represents a segment of content in the generated component.
/// </summary>
public sealed class HtmlSegment
{
    /// <summary>
    /// Type of segment
    /// </summary>
    public required SegmentType Type { get; init; }

    /// <summary>
    /// Content text (HTML for StaticHtml, expression for RazorExpression)
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Component reference details (only for ComponentReference type)
    /// </summary>
    public ComponentReference? Component { get; init; }

    /// <summary>
    /// Source location for diagnostics
    /// </summary>
    public SourceLocation? Location { get; init; }
}
