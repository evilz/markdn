namespace Markdn.Api.Models;

/// <summary>
/// Defines types of validation warnings for content items.
/// </summary>
public enum ValidationWarningType
{
    /// <summary>
    /// Content contains fields not defined in the schema.
    /// </summary>
    ExtraField,

    /// <summary>
    /// A field value is valid but may have issues (e.g., deprecated format).
    /// </summary>
    DeprecatedField,

    /// <summary>
    /// Other validation warning not covered by specific types.
    /// </summary>
    Other
}
