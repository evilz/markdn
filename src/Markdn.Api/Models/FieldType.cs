namespace Markdn.Api.Models;

/// <summary>
/// Defines the supported data types for collection schema fields.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Text string value.
    /// </summary>
    String,

    /// <summary>
    /// Numeric value (integer or decimal).
    /// </summary>
    Number,

    /// <summary>
    /// Boolean true/false value.
    /// </summary>
    Boolean,

    /// <summary>
    /// Date or date-time value.
    /// </summary>
    Date,

    /// <summary>
    /// Array of items.
    /// </summary>
    Array
}
