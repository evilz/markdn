namespace Markdn.Api.Models;

/// <summary>
/// Response DTO for single content item retrieval.
/// </summary>
public class ContentItemResponse
{
    /// <summary>
    /// Gets or sets the URL-safe unique identifier.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Gets or sets the content title from front-matter.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the publication date (ISO 8601).
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the author name.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the primary category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the short description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets custom front-matter fields.
    /// </summary>
    public Dictionary<string, object> CustomFields { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw Markdown body.
    /// </summary>
    public required string MarkdownContent { get; set; }

    /// <summary>
    /// Gets or sets the rendered HTML (null if format=markdown).
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Gets or sets the file last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets parsing warnings (present only if errors occurred).
    /// </summary>
    public List<string>? Warnings { get; set; }
}
