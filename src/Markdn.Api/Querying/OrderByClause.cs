using Markdn.Api.Models;

namespace Markdn.Api.Querying;

/// <summary>
/// Represents a single ordering clause for sorting query results.
/// </summary>
public class OrderByClause
{
    /// <summary>
    /// The field name to sort by.
    /// </summary>
    public required string FieldName { get; set; }

    /// <summary>
    /// The sort direction (ascending or descending).
    /// </summary>
    public required SortDirection Direction { get; set; }
}
