namespace Markdn.Api.Models;

/// <summary>
/// Request DTO for content query with filtering parameters.
/// </summary>
public class ContentQueryRequest
{
    /// <summary>
    /// Gets or sets the tag filter.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the date range start (ISO 8601).
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Gets or sets the date range end (ISO 8601).
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-indexed, default: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the items per page (default: 50, max: 100).
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the field to sort by (date, title, lastModified).
    /// </summary>
    public string SortBy { get; set; } = "date";

    /// <summary>
    /// Gets or sets the sort direction (asc, desc).
    /// </summary>
    public string SortOrder { get; set; } = "desc";
}
