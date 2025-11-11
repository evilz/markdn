using System.Text.Json.Serialization;

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
/// This type is tolerant to legacy property names used in test fixtures (e.g. "folderPath").
/// </summary>
public class CollectionDefinition
{
    /// <summary>
    /// Gets or sets the relative folder path from content root.
    /// </summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// Backward-compatible setter for legacy fixtures that use the property name "folderPath".
    /// When present in JSON, it will populate <see cref="Folder"/>.
    /// </summary>
    [JsonPropertyName("folderPath")]
    public string? FolderPath
    {
        get => Folder;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Folder = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the JSON Schema definition as a dynamic object.
    /// This will be parsed into a proper schema at runtime.
    /// </summary>
    public object Schema { get; set; } = new { };
}
