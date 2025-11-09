namespace Markdn.Api.Models;

/// <summary>
/// Represents a queryable, paginated collection of content items.
/// </summary>
public class ContentCollection
{
    /// <summary>
    /// Gets or sets the content items for the current page.
    /// </summary>
    public List<ContentItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total items matching the query.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-indexed).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets whether a previous page exists.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Gets whether a next page exists.
    /// </summary>
    public bool HasNext => Page < TotalPages;
}
