namespace Markdn.Api.Models;

/// <summary>
/// Response DTO for paginated content list.
/// </summary>
public class ContentListResponse
{
    /// <summary>
    /// Gets or sets the content items for the current page.
    /// </summary>
    public List<ContentItemSummary> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public required PaginationMetadata Pagination { get; set; }
}
