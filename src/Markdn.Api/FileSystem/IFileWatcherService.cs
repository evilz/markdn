namespace Markdn.Api.FileSystem;

/// <summary>
/// Interface for monitoring file system changes in the content directory.
/// </summary>
public interface IFileWatcherService
{
    /// <summary>
    /// Occurs when a markdown file is created.
    /// </summary>
    event EventHandler<string>? FileCreated;

    /// <summary>
    /// Occurs when a markdown file is modified.
    /// </summary>
    event EventHandler<string>? FileChanged;

    /// <summary>
    /// Occurs when a markdown file is deleted.
    /// </summary>
    event EventHandler<string>? FileDeleted;

    /// <summary>
    /// Starts monitoring the content directory for changes.
    /// </summary>
    void StartWatching();

    /// <summary>
    /// Stops monitoring the content directory for changes.
    /// </summary>
    void StopWatching();
}
