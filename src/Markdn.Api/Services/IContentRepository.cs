namespace Markdn.Api.Services;

/// <summary>
/// Repository interface for content operations
/// </summary>
public interface IContentRepository
{
    Task<Models.ContentCollection> GetAllAsync(CancellationToken cancellationToken);
    Task<Models.ContentItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
