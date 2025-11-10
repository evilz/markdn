namespace Markdn.Api.Querying;

/// <summary>
/// Represents a parsed OData-like query expression with filtering, sorting, and pagination.
/// </summary>
public class QueryExpression
{
    /// <summary>
    /// Filter expression ($filter clause).
    /// </summary>
    public FilterExpression? Filter { get; set; }

    /// <summary>
    /// List of ordering clauses ($orderby clause).
    /// </summary>
    public List<OrderByClause> OrderBy { get; set; } = new();

    /// <summary>
    /// Maximum number of items to return ($top clause).
    /// </summary>
    public int? Top { get; set; }

    /// <summary>
    /// Number of items to skip ($skip clause).
    /// </summary>
    public int? Skip { get; set; }
}
