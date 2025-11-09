using Markdn.Api.Models;

namespace Markdn.Api.Querying;

/// <summary>
/// Represents a comparison filter expression (e.g., author eq 'John', age gt 18).
/// </summary>
public class ComparisonExpression : FilterExpression
{
    /// <summary>
    /// The field name to compare.
    /// </summary>
    public required string FieldName { get; set; }

    /// <summary>
    /// The comparison operator.
    /// </summary>
    public required ComparisonOperator Operator { get; set; }

    /// <summary>
    /// The value to compare against.
    /// </summary>
    public required object Value { get; set; }

    /// <inheritdoc/>
    public override bool Evaluate(Dictionary<string, object?> item)
    {
        if (!item.TryGetValue(FieldName, out var fieldValue))
        {
            return false;
        }

        if (fieldValue == null)
        {
            return Value == null && Operator == ComparisonOperator.Equal;
        }

        return Operator switch
        {
            ComparisonOperator.Equal => AreEqual(fieldValue, Value),
            ComparisonOperator.NotEqual => !AreEqual(fieldValue, Value),
            ComparisonOperator.GreaterThan => Compare(fieldValue, Value) > 0,
            ComparisonOperator.GreaterThanOrEqual => Compare(fieldValue, Value) >= 0,
            ComparisonOperator.LessThan => Compare(fieldValue, Value) < 0,
            ComparisonOperator.LessThanOrEqual => Compare(fieldValue, Value) <= 0,
            ComparisonOperator.Contains => Contains(fieldValue, Value),
            ComparisonOperator.StartsWith => StartsWith(fieldValue, Value),
            ComparisonOperator.EndsWith => EndsWith(fieldValue, Value),
            _ => false
        };
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        // Handle string comparison case-insensitive
        if (left is string leftStr && right is string rightStr)
        {
            return leftStr.Equals(rightStr, StringComparison.OrdinalIgnoreCase);
        }

        // Try convert and compare
        try
        {
            var leftComparable = Convert.ChangeType(left, typeof(IComparable));
            var rightComparable = Convert.ChangeType(right, left.GetType());
            return leftComparable.Equals(rightComparable);
        }
        catch
        {
            return left.Equals(right);
        }
    }

    private static int Compare(object left, object right)
    {
        try
        {
            if (left is IComparable leftComparable)
            {
                var rightConverted = Convert.ChangeType(right, left.GetType());
                return leftComparable.CompareTo(rightConverted);
            }
        }
        catch
        {
            // Fall through to default
        }

        return string.Compare(left?.ToString(), right?.ToString(), StringComparison.Ordinal);
    }

    private static bool Contains(object? fieldValue, object value)
    {
        var fieldStr = fieldValue?.ToString();
        var valueStr = value?.ToString();
        
        if (fieldStr == null || valueStr == null)
            return false;

        return fieldStr.Contains(valueStr, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWith(object? fieldValue, object value)
    {
        var fieldStr = fieldValue?.ToString();
        var valueStr = value?.ToString();
        
        if (fieldStr == null || valueStr == null)
            return false;

        return fieldStr.StartsWith(valueStr, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndsWith(object? fieldValue, object value)
    {
        var fieldStr = fieldValue?.ToString();
        var valueStr = value?.ToString();
        
        if (fieldStr == null || valueStr == null)
            return false;

        return fieldStr.EndsWith(valueStr, StringComparison.OrdinalIgnoreCase);
    }
}
