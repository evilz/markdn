using Markdn.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Markdn.Api.FileSystem;

/// <summary>
/// Service for monitoring file system changes in the content directory with debouncing.
/// </summary>
public class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly FileSystemWatcher? _watcher;
    private readonly ILogger<FileWatcherService> _logger;
    private readonly Dictionary<string, Timer> _debounceTimers;
    private readonly object _timerLock = new();
    private const int DebounceDelayMs = 500;

    public event EventHandler<string>? FileCreated;
    public event EventHandler<string>? FileChanged;
    public event EventHandler<string>? FileDeleted;

    public FileWatcherService(
        IOptions<MarkdnOptions> options,
        ILogger<FileWatcherService> logger)
    {
        _logger = logger;
        _debounceTimers = new Dictionary<string, Timer>();

        var contentDirectory = options.Value.ContentDirectory;
        if (!Directory.Exists(contentDirectory))
        {
            _logger.LogWarning("Content directory does not exist: {Directory}", contentDirectory);
            return;
        }

        _watcher = new FileSystemWatcher
        {
            Path = contentDirectory,
            Filter = "*.md",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = false
        };

        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
    }

    /// <inheritdoc/>
    public void StartWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = true;
            _logger.LogInformation("Started watching content directory: {Path}", _watcher.Path);
        }
    }

    /// <inheritdoc/>
    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation("Stopped watching content directory");
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        DebounceEvent(e.FullPath, () =>
        {
            _logger.LogInformation("File created: {Path}", e.FullPath);
            FileCreated?.Invoke(this, e.FullPath);
        });
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        DebounceEvent(e.FullPath, () =>
        {
            _logger.LogInformation("File changed: {Path}", e.FullPath);
            FileChanged?.Invoke(this, e.FullPath);
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        // No debouncing for deletions - they're immediate
        _logger.LogInformation("File deleted: {Path}", e.FullPath);
        FileDeleted?.Invoke(this, e.FullPath);
    }

    private void DebounceEvent(string filePath, Action action)
    {
        lock (_timerLock)
        {
            // Cancel existing timer for this file
            if (_debounceTimers.TryGetValue(filePath, out var existingTimer))
            {
                existingTimer.Dispose();
            }

            // Create new timer that fires after delay
            var timer = new Timer(_ =>
            {
                action();
                lock (_timerLock)
                {
                    if (_debounceTimers.TryGetValue(filePath, out var t))
                    {
                        t.Dispose();
                        _debounceTimers.Remove(filePath);
                    }
                }
            }, null, DebounceDelayMs, Timeout.Infinite);

            _debounceTimers[filePath] = timer;
        }
    }

    public void Dispose()
    {
        lock (_timerLock)
        {
            foreach (var timer in _debounceTimers.Values)
            {
                timer.Dispose();
            }
            _debounceTimers.Clear();
        }

        if (_watcher != null)
        {
            _watcher.Created -= OnFileCreated;
            _watcher.Changed -= OnFileChanged;
            _watcher.Deleted -= OnFileDeleted;
            _watcher.Dispose();
        }
    }
}
