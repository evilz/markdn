using Markdn.Api.Models;

namespace Markdn.Api.Models;

/// <summary>
/// Response model for collection items query.
/// </summary>
public class CollectionItemsResponse
{
    /// <summary>
    /// The list of content items matching the query.
    /// </summary>
    public List<ContentItem> Items { get; set; } = new();

    /// <summary>
    /// The total count of items in the collection.
    /// </summary>
    public int TotalCount { get; set; }
}
