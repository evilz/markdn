using Markdn.Api.Configuration;
using Markdn.Api.Services;
using Microsoft.Extensions.Options;

namespace Markdn.Api.FileSystem;

/// <summary>
/// Hosted service that manages the file watcher lifecycle and coordinates with the content cache.
/// </summary>
public class FileWatcherHostedService : IHostedService
{
    private readonly IFileWatcherService _fileWatcher;
    private readonly IContentCache _cache;
    private readonly IOptions<MarkdnOptions> _options;
    private readonly ILogger<FileWatcherHostedService> _logger;

    public FileWatcherHostedService(
        IFileWatcherService fileWatcher,
        IContentCache cache,
        IOptions<MarkdnOptions> options,
        ILogger<FileWatcherHostedService> logger)
    {
        _fileWatcher = fileWatcher;
        _cache = cache;
        _options = options;
        _logger = logger;

        // Subscribe to file watcher events
        _fileWatcher.FileCreated += OnFileCreated;
        _fileWatcher.FileChanged += OnFileChanged;
        _fileWatcher.FileDeleted += OnFileDeleted;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.Value.EnableFileWatching)
        {
            _fileWatcher.StartWatching();
            _logger.LogInformation("File watching enabled");
        }
        else
        {
            _logger.LogInformation("File watching disabled");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _fileWatcher.StopWatching();
        _logger.LogInformation("File watcher stopped");
        return Task.CompletedTask;
    }

    private void OnFileCreated(object? sender, string filePath)
    {
        var slug = Path.GetFileNameWithoutExtension(filePath);
        _logger.LogInformation("Cache refresh triggered by file creation: {Slug}", slug);
        _ = _cache.RefreshAsync(slug);
    }

    private void OnFileChanged(object? sender, string filePath)
    {
        var slug = Path.GetFileNameWithoutExtension(filePath);
        _logger.LogInformation("Cache invalidation triggered by file change: {Slug}", slug);
        _cache.Invalidate(slug);
    }

    private void OnFileDeleted(object? sender, string filePath)
    {
        var slug = Path.GetFileNameWithoutExtension(filePath);
        _logger.LogInformation("Cache invalidation triggered by file deletion: {Slug}", slug);
        _cache.Invalidate(slug);
    }
}
