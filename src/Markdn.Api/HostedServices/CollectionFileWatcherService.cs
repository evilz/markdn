using Markdn.Api.Configuration;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Markdn.Api.HostedServices;

/// <summary>
/// Hosted service that monitors collection content directories for file changes
/// and invalidates the cache to ensure fresh data.
/// </summary>
public class CollectionFileWatcherService : IHostedService, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICollectionLoader _collectionLoader;
    private readonly IOptions<MarkdnOptions> _options;
    private readonly ILogger<CollectionFileWatcherService> _logger;
    private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new();
    private const int DebounceDelayMs = 300;

    public CollectionFileWatcherService(
        IMemoryCache memoryCache,
        ICollectionLoader collectionLoader,
        IOptions<MarkdnOptions> options,
        ILogger<CollectionFileWatcherService> logger)
    {
        _memoryCache = memoryCache;
        _collectionLoader = collectionLoader;
        _options = options;
        _logger = logger;

        // Subscribe to configuration changes
        if (_collectionLoader is CollectionLoader loader)
        {
            loader.ConfigurationChanged += OnCollectionConfigurationChanged;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.EnableFileWatching)
        {
            _logger.LogInformation("Collection file watching disabled by configuration");
            return;
        }

        try
        {
            // Load all collections to get their content directories
            var collections = await _collectionLoader.LoadCollectionsAsync(cancellationToken);

            foreach (var kvp in collections)
            {
                var name = kvp.Key;
                var collection = kvp.Value;
                var contentDirectory = Path.Combine(_options.Value.ContentDirectory, collection.FolderPath);

                if (!Directory.Exists(contentDirectory))
                {
                    _logger.LogWarning("Content directory does not exist for collection {CollectionName}: {Path}",
                        name, contentDirectory);
                    continue;
                }

                var watcher = new FileSystemWatcher(contentDirectory)
                {
                    Filter = "*.md",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };

                watcher.Created += (sender, e) => OnFileChanged(name, e.FullPath, "created");
                watcher.Changed += (sender, e) => OnFileChanged(name, e.FullPath, "changed");
                watcher.Deleted += (sender, e) => OnFileChanged(name, e.FullPath, "deleted");
                watcher.Renamed += (sender, e) => OnFileChanged(name, e.FullPath, "renamed");

                _watchers[name] = watcher;

                _logger.LogInformation("Started watching collection {CollectionName} at {Path}",
                    name, contentDirectory);
            }

            _logger.LogInformation("Collection file watching enabled for {Count} collections",
                _watchers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting collection file watchers");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var (collectionName, watcher) in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _logger.LogInformation("Stopped watching collection {CollectionName}", collectionName);
        }

        _watchers.Clear();

        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }

        _debounceTimers.Clear();

        _logger.LogInformation("Collection file watcher stopped");
        return Task.CompletedTask;
    }

    private void OnFileChanged(string collectionName, string filePath, string changeType)
    {
        var fileName = Path.GetFileName(filePath);
        var debounceKey = $"{collectionName}:{fileName}";

        // Dispose existing timer if it exists
        if (_debounceTimers.TryRemove(debounceKey, out var existingTimer))
        {
            existingTimer.Dispose();
        }

        // Create new debounce timer
        var timer = new Timer(_ =>
        {
            try
            {
                _logger.LogInformation(
                    "File {ChangeType} detected in collection {CollectionName}: {FileName}",
                    changeType, collectionName, fileName);

                // Invalidate cache for this collection
                var cacheKey = $"collection_items_{collectionName}";
                _memoryCache.Remove(cacheKey);

                _logger.LogDebug("Cache invalidated for collection {CollectionName}", collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error invalidating cache for collection {CollectionName} after file {ChangeType}",
                    collectionName, changeType);
            }
            finally
            {
                // Clean up timer after execution
                if (_debounceTimers.TryRemove(debounceKey, out var timerToDispose))
                {
                    timerToDispose.Dispose();
                }
            }
        }, null, DebounceDelayMs, Timeout.Infinite);

        _debounceTimers[debounceKey] = timer;
    }

    private void OnCollectionConfigurationChanged(object? sender, string configPath)
    {
        _logger.LogInformation("Collections configuration changed at {ConfigPath}, invalidating all collection caches",
            configPath);

        // Invalidate all collection item caches
        foreach (var collectionName in _watchers.Keys)
        {
            var cacheKey = $"collection_items_{collectionName}";
            _memoryCache.Remove(cacheKey);
            _logger.LogDebug("Invalidated cache for collection {CollectionName}", collectionName);
        }

        // Restart watchers with new configuration
        _ = Task.Run(async () =>
        {
            try
            {
                // Stop existing watchers
                foreach (var (name, watcher) in _watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                _watchers.Clear();

                _logger.LogInformation("Restarting collection file watchers with new configuration");

                // Restart with new configuration
                await StartAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting collection file watchers after configuration change");
            }
        });
    }

    public void Dispose()
    {
        // Unsubscribe from configuration changes
        if (_collectionLoader is CollectionLoader loader)
        {
            loader.ConfigurationChanged -= OnCollectionConfigurationChanged;
        }

        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }

        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }

        _watchers.Clear();
        _debounceTimers.Clear();
    }
}
