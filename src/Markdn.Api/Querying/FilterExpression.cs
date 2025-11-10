namespace Markdn.Api.Querying;

/// <summary>
/// Abstract base class for filter expressions in queries.
/// Implements Composite pattern for building complex filter logic.
/// </summary>
public abstract class FilterExpression
{
    /// <summary>
    /// Evaluates this filter expression against a content item.
    /// </summary>
    /// <param name="item">The content item to evaluate.</param>
    /// <returns>True if the item matches the filter, false otherwise.</returns>
    public abstract bool Evaluate(Dictionary<string, object?> item);
}
