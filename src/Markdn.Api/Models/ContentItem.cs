namespace Markdn.Api.Models;

/// <summary>
/// Represents a single Markdown document with its metadata and content.
/// </summary>
public class ContentItem
{
    /// <summary>
    /// Gets or sets the URL-safe unique identifier.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the source file.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the content title from front-matter.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the publication date (ISO 8601).
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the author name from front-matter.
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
    /// Gets or sets the short description/summary.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets additional front-matter fields.
    /// </summary>
    public Dictionary<string, object> CustomFields { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw Markdown body.
    /// </summary>
    public string? MarkdownContent { get; set; }

    /// <summary>
    /// Gets or sets the rendered HTML (lazy-loaded).
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Gets or sets the file last modified timestamp.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets whether YAML parsing failed.
    /// </summary>
    public bool HasParsingErrors { get; set; }

    /// <summary>
    /// Gets or sets error messages if any.
    /// </summary>
    public List<string> ParsingWarnings { get; set; } = new();
}
