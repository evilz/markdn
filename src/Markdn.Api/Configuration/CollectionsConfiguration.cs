namespace Markdn.Api.Configuration;

/// <summary>
/// Configuration model for collections.json file.
/// </summary>
public class CollectionsConfiguration
{
    /// <summary>
    /// Gets or sets the relative path to the content root directory.
    /// </summary>
    public string ContentRootPath { get; set; } = "content";

    /// <summary>
    /// Gets or sets the collection definitions keyed by collection name.
    /// </summary>
    public Dictionary<string, CollectionDefinition> Collections { get; set; } = new();
}

/// <summary>
/// Defines a single collection with its folder and schema.
/// </summary>
public class CollectionDefinition
{
    /// <summary>
    /// Gets or sets the relative folder path from content root.
    /// </summary>
    public required string Folder { get; set; }

    /// <summary>
    /// Gets or sets the JSON Schema definition as a dynamic object.
    /// This will be parsed into a proper schema at runtime.
    /// </summary>
    public required object Schema { get; set; }
}
