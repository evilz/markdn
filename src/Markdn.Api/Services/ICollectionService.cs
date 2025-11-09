using Markdn.Api.Models;
using Markdn.Api.Querying;

namespace Markdn.Api.Services;

/// <summary>
/// Service for querying and retrieving content items from collections.
/// Provides type-safe access to validated collection content.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Gets all validated content items from a collection.
    /// </summary>
    /// <param name="collectionName">The name of the collection to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A list of validated content items, or empty list if collection doesn't exist.</returns>
    Task<IReadOnlyList<ContentItem>> GetAllItemsAsync(
        string collectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single content item by its slug or identifier.
    /// Resolves slug from front-matter or filename.
    /// </summary>
    /// <param name="collectionName">The name of the collection to query.</param>
    /// <param name="slug">The slug or identifier of the content item.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The content item if found and valid, otherwise null.</returns>
    Task<ContentItem?> GetItemByIdAsync(
        string collectionName,
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries a collection with advanced filtering, sorting, and pagination.
    /// </summary>
    /// <param name="collectionName">The name of the collection to query.</param>
    /// <param name="query">The query expression with filters, sorting, and pagination.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A list of content items matching the query.</returns>
    Task<IReadOnlyList<ContentItem>> QueryAsync(
        string collectionName,
        QueryExpression query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for all available collections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A dictionary of collection names to collection metadata.</returns>
    Task<IReadOnlyDictionary<string, Collection>> GetAllCollectionsAsync(
        CancellationToken cancellationToken = default);
}
