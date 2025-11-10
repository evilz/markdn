namespace Markdn.Api.Models;

/// <summary>
/// Detailed information about a validation warning (non-blocking issue).
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Gets or sets the name of the field that triggered the warning.
    /// </summary>
    public required string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the type of validation warning.
    /// </summary>
    public required ValidationWarningType WarningType { get; set; }

    /// <summary>
    /// Gets or sets the human-readable warning description.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the actual value that triggered the warning.
    /// </summary>
    public object? ActualValue { get; set; }
}
