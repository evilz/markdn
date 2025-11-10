namespace Markdn.Api.Querying;

/// <summary>
/// Logical operator for combining filter expressions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Logical AND - both expressions must be true.
    /// </summary>
    And,

    /// <summary>
    /// Logical OR - at least one expression must be true.
    /// </summary>
    Or
}

/// <summary>
/// Represents a logical filter expression combining two sub-expressions with AND/OR.
/// </summary>
public class LogicalExpression : FilterExpression
{
    /// <summary>
    /// The left-hand filter expression.
    /// </summary>
    public required FilterExpression Left { get; set; }

    /// <summary>
    /// The logical operator (AND/OR).
    /// </summary>
    public required LogicalOperator Operator { get; set; }

    /// <summary>
    /// The right-hand filter expression.
    /// </summary>
    public required FilterExpression Right { get; set; }

    /// <inheritdoc/>
    public override bool Evaluate(Dictionary<string, object?> item)
    {
        return Operator switch
        {
            LogicalOperator.And => Left.Evaluate(item) && Right.Evaluate(item),
            LogicalOperator.Or => Left.Evaluate(item) || Right.Evaluate(item),
            _ => false
        };
    }
}
