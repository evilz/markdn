namespace Markdn.Api.Models;

/// <summary>
/// Defines comparison operators for query filter expressions.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// Equal to (eq).
    /// </summary>
    Equal,

    /// <summary>
    /// Not equal to (ne).
    /// </summary>
    NotEqual,

    /// <summary>
    /// Greater than (gt).
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal to (ge).
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than (lt).
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal to (le).
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Contains substring (for strings).
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with substring (for strings).
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with substring (for strings).
    /// </summary>
    EndsWith
}
