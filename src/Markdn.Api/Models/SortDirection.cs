namespace Markdn.Api.Models;

/// <summary>
/// Defines sort direction for query ordering.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Sort in ascending order (A-Z, 0-9, oldest-newest).
    /// </summary>
    Ascending,

    /// <summary>
    /// Sort in descending order (Z-A, 9-0, newest-oldest).
    /// </summary>
    Descending
}
