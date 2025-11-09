namespace Markdn.Api.Models;

/// <summary>
/// Pagination metadata for list responses.
/// </summary>
public class PaginationMetadata
{
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
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets whether a previous page exists.
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Gets or sets whether a next page exists.
    /// </summary>
    public bool HasNext { get; set; }
}
