namespace Markdn.Api.Models;

/// <summary>
/// Summary DTO for content items in list responses.
/// </summary>
public class ContentItemSummary
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
    /// Gets or sets the file last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; }
}
