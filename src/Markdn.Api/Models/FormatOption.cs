namespace Markdn.Api.Models;

/// <summary>
/// Specifies the format options for content rendering.
/// </summary>
public enum FormatOption
{
    /// <summary>
    /// Return only the raw Markdown content.
    /// </summary>
    Markdown,

    /// <summary>
    /// Return only the rendered HTML content.
    /// </summary>
    Html,

    /// <summary>
    /// Return both Markdown and HTML content (default).
    /// </summary>
    Both
}
