namespace Markdn.Api.Models;

/// <summary>
/// Defines types of validation errors for content items.
/// </summary>
public enum ValidationErrorType
{
    /// <summary>
    /// A required field is missing from the content.
    /// </summary>
    MissingRequiredField,

    /// <summary>
    /// A field value does not match the expected type.
    /// </summary>
    TypeMismatch,

    /// <summary>
    /// A string value exceeds maximum length constraint.
    /// </summary>
    MaxLengthExceeded,

    /// <summary>
    /// A string value is below minimum length constraint.
    /// </summary>
    MinLengthViolation,

    /// <summary>
    /// A string value does not match the required regex pattern.
    /// </summary>
    PatternMismatch,

    /// <summary>
    /// A numeric value exceeds the maximum allowed.
    /// </summary>
    MaximumExceeded,

    /// <summary>
    /// A numeric value is below the minimum allowed.
    /// </summary>
    MinimumViolation,

    /// <summary>
    /// A value is not in the allowed enumeration set.
    /// </summary>
    EnumViolation,

    /// <summary>
    /// The JSON Schema itself is invalid.
    /// </summary>
    InvalidSchema,

    /// <summary>
    /// Other validation error not covered by specific types.
    /// </summary>
    Other
}
