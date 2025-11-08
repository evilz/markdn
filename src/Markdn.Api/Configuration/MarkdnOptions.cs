namespace Markdn.Api.Configuration;

/// <summary>
/// Configuration options for the Markdn CMS.
/// </summary>
public class MarkdnOptions
{
    /// <summary>
    /// Gets or sets the directory path containing Markdown content files.
    /// </summary>
    public string ContentDirectory { get; set; } = "content";

    /// <summary>
    /// Gets or sets the maximum file size in bytes (default: 5MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5_242_880; // 5MB

    /// <summary>
    /// Gets or sets the default page size for paginated responses.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether file system watching is enabled for live updates.
    /// </summary>
    public bool EnableFileWatching { get; set; } = true;
}
