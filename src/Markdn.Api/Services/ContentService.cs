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
        return await GetAllAsync(new ContentQueryRequest { Page = page, PageSize = pageSize }, page, pageSize, cancellationToken);
    }

    public async Task<ContentCollection> GetAllAsync(ContentQueryRequest query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var allContent = await _repository.GetAllAsync(cancellationToken);
        var items = allContent.Items.AsEnumerable();

        // Apply filtering
        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            items = items.Where(item => item.Tags.Contains(query.Tag, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            items = items.Where(item => 
                item.Category != null && 
                item.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (query.DateFrom.HasValue)
        {
            items = items.Where(item => item.Date.HasValue && item.Date.Value >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            items = items.Where(item => item.Date.HasValue && item.Date.Value <= query.DateTo.Value);
        }

        // Apply sorting
        items = ApplySorting(items, query.SortBy, query.SortOrder);

        var itemsList = items.ToList();

        // Apply pagination
        var skip = (page - 1) * pageSize;
        var pagedItems = itemsList.Skip(skip).Take(pageSize).ToList();

        return new ContentCollection
        {
            Items = pagedItems,
            TotalCount = itemsList.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IEnumerable<ContentItem> ApplySorting(IEnumerable<ContentItem> items, string sortBy, string sortOrder)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "date" => isDescending 
                ? items.OrderByDescending(i => i.Date ?? DateTime.MinValue)
                : items.OrderBy(i => i.Date ?? DateTime.MinValue),
            "title" => isDescending
                ? items.OrderByDescending(i => i.Title ?? string.Empty)
                : items.OrderBy(i => i.Title ?? string.Empty),
            "lastmodified" => isDescending
                ? items.OrderByDescending(i => i.LastModified)
                : items.OrderBy(i => i.LastModified),
            _ => items // No sorting if invalid sortBy
        };
    }

    public Task<ContentItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return _repository.GetBySlugAsync(slug, cancellationToken);
    }
}
