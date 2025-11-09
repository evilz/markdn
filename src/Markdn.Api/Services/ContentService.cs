using Markdn.Api.Models;
using Microsoft.Extensions.Logging;

namespace Markdn.Api.Services;

/// <summary>
/// Service for content operations with filtering, sorting, and pagination
/// </summary>
public class ContentService
{
    private readonly IContentRepository _repository;
    private readonly IContentCache _cache;
    private readonly ILogger<ContentService> _logger;

    /// <summary>
    /// Initializes a new instance of the ContentService
    /// </summary>
    /// <param name="repository">Content repository implementation</param>
    /// <param name="cache">Content cache implementation</param>
    /// <param name="logger">Logger instance</param>
    public ContentService(IContentRepository repository, IContentCache cache, ILogger<ContentService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all content with pagination
    /// </summary>
    /// <param name="page">Page number (1-indexed)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated content collection</returns>
    public async Task<ContentCollection> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await GetAllAsync(new ContentQueryRequest { Page = page, PageSize = pageSize }, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Retrieves content with filtering, sorting, and pagination
    /// </summary>
    /// <param name="query">Query request with filter and sort parameters</param>
    /// <param name="page">Page number (1-indexed)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered and sorted paginated content collection</returns>
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

    /// <summary>
    /// Applies sorting to content items based on the specified field and order
    /// </summary>
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

    /// <summary>
    /// Retrieves a single content item by its slug
    /// </summary>
    /// <param name="slug">Unique slug identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content item if found, null otherwise</returns>
    public Task<ContentItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return GetBySlugAsync(slug, FormatOption.Both, cancellationToken);
    }

    /// <summary>
    /// Retrieves a single content item by its slug with specified format option
    /// </summary>
    /// <param name="slug">Unique slug identifier</param>
    /// <param name="format">Format option for content rendering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content item if found with requested format, null otherwise</returns>
    public async Task<ContentItem?> GetBySlugAsync(string slug, FormatOption format, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving content for slug: {Slug} with format: {Format}", slug, format);

        // Cache-aside pattern: check cache first
        var cachedItem = _cache.Get(slug);
        if (cachedItem != null)
        {
            _logger.LogDebug("Content found in cache for slug: {Slug}", slug);
            return ApplyFormatOption(cachedItem, format);
        }

        // Not in cache, fetch from repository
        var item = await _repository.GetBySlugAsync(slug, cancellationToken);
        if (item != null)
        {
            _cache.Set(slug, item);
            _logger.LogDebug("Content loaded and cached for slug: {Slug}", slug);
            return ApplyFormatOption(item, format);
        }

        _logger.LogDebug("Content not found for slug: {Slug}", slug);
        return null;
    }

    /// <summary>
    /// Applies the format option to the content item
    /// </summary>
    /// <param name="item">The content item</param>
    /// <param name="format">The format option</param>
    /// <returns>Content item with requested format</returns>
    private ContentItem ApplyFormatOption(ContentItem item, FormatOption format)
    {
        return format switch
        {
            FormatOption.Markdown => new ContentItem
            {
                Slug = item.Slug,
                Title = item.Title,
                Date = item.Date,
                Author = item.Author,
                Category = item.Category,
                Tags = item.Tags,
                Description = item.Description,
                FilePath = item.FilePath,
                MarkdownContent = item.MarkdownContent,
                HtmlContent = null, // Exclude HTML when format=Markdown
                LastModified = item.LastModified
            },
            FormatOption.Html => new ContentItem
            {
                Slug = item.Slug,
                Title = item.Title,
                Date = item.Date,
                Author = item.Author,
                Category = item.Category,
                Tags = item.Tags,
                Description = item.Description,
                FilePath = item.FilePath,
                MarkdownContent = null, // Exclude Markdown when format=Html
                HtmlContent = item.HtmlContent,
                LastModified = item.LastModified
            },
            FormatOption.Both => item, // Return both formats
            _ => item
        };
    }
}
