using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Markdn.Api.Services;

/// <summary>
/// In-memory cache implementation for content items.
/// </summary>
public class ContentCache : IContentCache
{
    private readonly IMemoryCache _cache;
    private readonly IContentRepository _repository;
    private readonly ILogger<ContentCache> _logger;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public ContentCache(
        IMemoryCache cache,
        IContentRepository repository,
        ILogger<ContentCache> logger)
    {
        _cache = cache;
        _repository = repository;
        _logger = logger;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            Size = 1
        };
    }

    /// <inheritdoc/>
    public ContentItem? Get(string slug)
    {
        return _cache.TryGetValue(slug, out ContentItem? item) ? item : null;
    }

    /// <inheritdoc/>
    public void Set(string slug, ContentItem item)
    {
        _cache.Set(slug, item, _cacheOptions);
        _logger.LogDebug("Cached content item: {Slug}", slug);
    }

    /// <inheritdoc/>
    public void Invalidate(string slug)
    {
        _cache.Remove(slug);
        _logger.LogDebug("Invalidated cache for: {Slug}", slug);
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(string slug, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetBySlugAsync(slug, cancellationToken);
        if (item != null)
        {
            Set(slug, item);
            _logger.LogInformation("Refreshed cache for: {Slug}", slug);
        }
        else
        {
            Invalidate(slug);
            _logger.LogWarning("Content not found during refresh: {Slug}", slug);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
            _logger.LogInformation("Cache cleared");
        }
    }
}
