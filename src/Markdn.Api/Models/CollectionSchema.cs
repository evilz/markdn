namespace Markdn.Api.Models;

/// <summary>
/// Defines the structure and validation rules for content in a collection.
/// </summary>
public class CollectionSchema
{
    /// <summary>
    /// Gets or sets the schema type (always "object" for content schemas).
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// Gets or sets the field definitions for this schema.
    /// </summary>
    public required Dictionary<string, FieldDefinition> Properties { get; set; }

    /// <summary>
    /// Gets or sets the names of required fields.
    /// </summary>
    public List<string> Required { get; set; } = new();

    /// <summary>
    /// Gets or sets whether extra fields not in the schema are allowed.
    /// Default is true per specification (preserve with warning).
    /// </summary>
    public bool AdditionalProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets the human-readable schema name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the schema documentation.
    /// </summary>
    public string? Description { get; set; }
}
