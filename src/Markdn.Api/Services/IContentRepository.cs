namespace Markdn.Api.Services;

/// <summary>
/// Repository interface for content storage and retrieval operations
/// </summary>
public interface IContentRepository
{
    /// <summary>
    /// Retrieves all content items from the repository
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all content items</returns>
    Task<Models.ContentCollection> GetAllAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves a single content item by its unique slug
    /// </summary>
    /// <param name="slug">Unique slug identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content item if found, null otherwise</returns>
    Task<Models.ContentItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
