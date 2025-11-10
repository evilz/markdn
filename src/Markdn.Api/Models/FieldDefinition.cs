namespace Markdn.Api.Models;

/// <summary>
/// Defines a single field in a collection schema with validation constraints.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// Gets or sets the field name (JSON property key).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the data type for this field.
    /// </summary>
    public required FieldType Type { get; set; }

    /// <summary>
    /// Gets or sets additional format constraint (e.g., "date", "email", "uri").
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the regex pattern for string validation.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the minimum string length (applies to String type only).
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum string length (applies to String type only).
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum numeric value (applies to Number type only).
    /// </summary>
    public decimal? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum numeric value (applies to Number type only).
    /// </summary>
    public decimal? Maximum { get; set; }

    /// <summary>
    /// Gets or sets the allowed values for enumerated types.
    /// </summary>
    public List<string>? Enum { get; set; }

    /// <summary>
    /// Gets or sets the schema for array items (required when Type = Array).
    /// </summary>
    public FieldDefinition? Items { get; set; }

    /// <summary>
    /// Gets or sets the field documentation.
    /// </summary>
    public string? Description { get; set; }
}
