namespace Markdn.Api.Models;

/// <summary>
/// Represents the YAML metadata section at the beginning of a Markdown file.
/// </summary>
public class FrontMatter
{
    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the publication date (string, parsed separately).
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    /// Gets or sets the author name.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the list of tags.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the primary category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the short description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the custom slug override.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Gets or sets dynamic/custom fields not in the standard schema.
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}
