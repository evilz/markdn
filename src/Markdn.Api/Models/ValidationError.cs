namespace Markdn.Api.Models;

/// <summary>
/// Detailed information about a validation failure.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the name of the field that failed validation.
    /// </summary>
    public required string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the type of validation error.
    /// </summary>
    public required ValidationErrorType ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the human-readable error description.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the expected data type (if applicable).
    /// </summary>
    public string? ExpectedType { get; set; }

    /// <summary>
    /// Gets or sets the actual value that failed validation.
    /// </summary>
    public object? ActualValue { get; set; }
}
