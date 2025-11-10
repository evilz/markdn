namespace Markdn.Api.Models;

/// <summary>
/// Outcome of validating a content item against a schema.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets the overall validation status.
    /// </summary>
    public required bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors (empty if valid).
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the non-blocking warnings (e.g., extra fields).
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation timestamp.
    /// </summary>
    public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
}
