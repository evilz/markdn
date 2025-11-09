using Markdn.Api.Models;

namespace Markdn.Api.Configuration;

/// <summary>
/// Options for Content Collections feature configured via IOptions pattern.
/// </summary>
public class CollectionsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Collections";

    /// <summary>
    /// Gets or sets the absolute path to the collections.json configuration file.
    /// Defaults to "content/collections.json" relative to content root.
    /// </summary>
    public string ConfigurationFilePath { get; set; } = "content/collections.json";

    /// <summary>
    /// Gets or sets whether to perform eager validation at startup.
    /// Default is true per specification.
    /// </summary>
    public bool EagerValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in seconds for eager validation.
    /// Default is 5 seconds per specification.
    /// </summary>
    public int ValidationTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to watch for file changes at runtime.
    /// Default is true.
    /// </summary>
    public bool EnableFileWatcher { get; set; } = true;

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds for file change events.
    /// Default is 300ms per specification.
    /// </summary>
    public int FileWatcherDebounceMs { get; set; } = 300;
}
