using Markdn.Api.Models;

namespace Markdn.Api.Services;

/// <summary>
/// Interface for caching content items in memory.
/// </summary>
public interface IContentCache
{
    /// <summary>
    /// Gets a content item from the cache by slug.
    /// </summary>
    /// <param name="slug">The content slug.</param>
    /// <returns>The cached content item, or null if not found.</returns>
    ContentItem? Get(string slug);

    /// <summary>
    /// Stores a content item in the cache.
    /// </summary>
    /// <param name="slug">The content slug.</param>
    /// <param name="item">The content item to cache.</param>
    void Set(string slug, ContentItem item);

    /// <summary>
    /// Invalidates (removes) a content item from the cache.
    /// </summary>
    /// <param name="slug">The content slug.</param>
    void Invalidate(string slug);

    /// <summary>
    /// Refreshes a content item in the cache by reloading it from the repository.
    /// </summary>
    /// <param name="slug">The content slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    void Clear();
}
