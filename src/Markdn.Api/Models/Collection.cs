namespace Markdn.Api.Models;

/// <summary>
/// Represents a group of related content files with a defined schema.
/// </summary>
public class Collection
{
    /// <summary>
    /// Gets or sets the unique identifier for the collection (e.g., "blog", "docs").
    /// Must be lowercase alphanumeric with hyphens.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the relative path to collection folder from content root.
    /// </summary>
    public required string FolderPath { get; set; }

    /// <summary>
    /// Gets or sets the JSON Schema definition for content validation.
    /// </summary>
    public required CollectionSchema Schema { get; set; }

    /// <summary>
    /// Gets or sets the validated content items in the collection.
    /// This is a computed property populated at runtime.
    /// </summary>
    public IReadOnlyList<ContentItem>? Items { get; set; }
}
