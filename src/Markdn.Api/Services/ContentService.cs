using Markdn.Api.Models;

namespace Markdn.Api.Services;

/// <summary>
/// Service for content operations
/// </summary>
public class ContentService
{
    private readonly IContentRepository _repository;

    public ContentService(IContentRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContentCollection> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var allContent = await _repository.GetAllAsync(cancellationToken);
        
        // Simple pagination (in-memory for now)
        var skip = (page - 1) * pageSize;
        var pagedItems = allContent.Items.Skip(skip).Take(pageSize).ToList();

        return new ContentCollection
        {
            Items = pagedItems,
            TotalCount = allContent.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<ContentItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return _repository.GetBySlugAsync(slug, cancellationToken);
    }
}
