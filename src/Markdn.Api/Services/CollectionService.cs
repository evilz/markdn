using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Markdn.Api.Services;

/// <summary>
/// Service for querying and retrieving validated content items from collections.
/// Provides type-safe access to collection content with caching support.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly ICollectionLoader _collectionLoader;
    private readonly FrontMatterParser _frontMatterParser;
    private readonly MarkdownParser _markdownParser;
    private readonly SlugGenerator _slugGenerator;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CollectionService> _logger;
    private readonly MarkdnOptions _options;
    private static readonly ActivitySource ActivitySource = new("Markdn.Api.CollectionService");

    public CollectionService(
        ICollectionLoader collectionLoader,
        FrontMatterParser frontMatterParser,
        MarkdownParser markdownParser,
        SlugGenerator slugGenerator,
        IMemoryCache cache,
        IOptions<MarkdnOptions> options,
        ILogger<CollectionService> logger)
    {
        _collectionLoader = collectionLoader;
        _frontMatterParser = frontMatterParser;
        _markdownParser = markdownParser;
        _slugGenerator = slugGenerator;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ContentItem>> GetAllItemsAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetAllItems");
        activity?.SetTag("collection.name", collectionName);

        _logger.LogInformation("Getting all items from collection {CollectionName}", collectionName);

        var cacheKey = $"collection_items_{collectionName}";
        
        if (_cache.TryGetValue<List<ContentItem>>(cacheKey, out var cachedItems) && cachedItems != null)
        {
            _logger.LogDebug("Returning {Count} cached items for collection {CollectionName}", 
                cachedItems.Count, collectionName);
            return cachedItems;
        }

        var collection = await _collectionLoader.LoadCollectionAsync(collectionName, cancellationToken)
            .ConfigureAwait(false);

        if (collection == null)
        {
            _logger.LogWarning("Collection {CollectionName} not found", collectionName);
            return Array.Empty<ContentItem>();
        }

        var items = await LoadCollectionItemsAsync(collection, cancellationToken).ConfigureAwait(false);
        
        // Validate slug uniqueness
        var slugGroups = items.GroupBy(i => i.Slug, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (slugGroups.Any())
        {
            foreach (var group in slugGroups)
            {
                _logger.LogWarning(
                    "Duplicate slug {Slug} found in collection {CollectionName}: {FilePaths}",
                    group.Key,
                    collectionName,
                    string.Join(", ", group.Select(i => i.FilePath)));
            }
        }

        // Cache for 5 minutes
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        _cache.Set(cacheKey, items, cacheOptions);

        _logger.LogInformation("Loaded {Count} items from collection {CollectionName}", 
            items.Count, collectionName);

        return items;
    }

    /// <inheritdoc/>
    public async Task<ContentItem?> GetItemByIdAsync(
        string collectionName,
        string slug,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetItemById");
        activity?.SetTag("collection.name", collectionName);
        activity?.SetTag("item.slug", slug);

        _logger.LogInformation("Getting item {Slug} from collection {CollectionName}", 
            slug, collectionName);

        var items = await GetAllItemsAsync(collectionName, cancellationToken).ConfigureAwait(false);
        
        var item = items.FirstOrDefault(i => 
            i.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            _logger.LogDebug("Item {Slug} not found in collection {CollectionName}", 
                slug, collectionName);
        }

        return item;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, Collection>> GetAllCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetAllCollections");

        _logger.LogInformation("Getting all collections");

        var collections = await _collectionLoader.LoadCollectionsAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Loaded {Count} collections", collections.Count);

        return collections;
    }

    private async Task<List<ContentItem>> LoadCollectionItemsAsync(
        Collection collection,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = new List<ContentItem>();

        var contentDirectory = Path.GetFullPath(_options.ContentDirectory);
        var collectionPath = Path.Combine(contentDirectory, collection.FolderPath);

        if (!Directory.Exists(collectionPath))
        {
            _logger.LogWarning("Collection folder {FolderPath} does not exist", collectionPath);
            return items;
        }

        var markdownFiles = Directory.GetFiles(collectionPath, "*.md", SearchOption.AllDirectories);
        var jsonFiles = Directory.GetFiles(collectionPath, "*.json", SearchOption.AllDirectories);
        var allFiles = markdownFiles.Concat(jsonFiles).ToArray();

        _logger.LogDebug("Found {FileCount} files in collection {CollectionName}", 
            allFiles.Length, collection.Name);

        foreach (var filePath in allFiles)
        {
            try
            {
                // Security: Verify file is within collection directory
                var fullPath = Path.GetFullPath(filePath);
                if (!fullPath.StartsWith(collectionPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Skipping file outside collection directory: {FilePath}", filePath);
                    continue;
                }

                var fileInfo = new FileInfo(filePath);

                // Skip files larger than max size
                if (fileInfo.Length > _options.MaxFileSizeBytes)
                {
                    _logger.LogWarning("Skipping file exceeding max size: {FilePath}", filePath);
                    continue;
                }

                var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                var item = await ParseContentItemAsync(filePath, content, fileInfo, collection, cancellationToken)
                    .ConfigureAwait(false);

                // Only include valid items
                if (item.IsValid)
                {
                    items.Add(item);
                }
                else
                {
                    _logger.LogWarning("Skipping invalid item: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading file {FilePath}", filePath);
                continue;
            }
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Loaded {ItemCount} valid items from {FileCount} files in collection {CollectionName} in {ElapsedMs}ms",
            items.Count, allFiles.Length, collection.Name, stopwatch.ElapsedMilliseconds);

        return items;
    }

    private async Task<ContentItem> ParseContentItemAsync(
        string filePath,
        string content,
        FileInfo fileInfo,
        Collection collection,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var hasErrors = false;

        // Parse front-matter
        FrontMatter frontMatter;
        try
        {
            frontMatter = await _frontMatterParser.ParseAsync(content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            frontMatter = new FrontMatter();
            warnings.Add($"Failed to parse front-matter: {ex.Message}");
            hasErrors = true;
        }

        // Extract markdown content (remove front-matter)
        var markdownContent = ExtractMarkdownContent(content);

        // Generate slug
        var fileName = Path.GetFileName(filePath);
        var slug = _slugGenerator.GenerateSlug(frontMatter.Slug, fileName);

        // Parse date
        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(frontMatter.Date))
        {
            if (DateTime.TryParse(frontMatter.Date, out var dateValue))
            {
                parsedDate = dateValue;
            }
            else
            {
                warnings.Add($"Failed to parse date: {frontMatter.Date}");
            }
        }

        // Parse to HTML
        string? htmlContent = null;
        try
        {
            htmlContent = await _markdownParser.ParseAsync(markdownContent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to parse markdown: {ex.Message}");
            hasErrors = true;
        }

        // Merge front-matter fields with additional properties
        var customFields = new Dictionary<string, object>(frontMatter.AdditionalProperties);
        if (!string.IsNullOrWhiteSpace(frontMatter.Title))
        {
            customFields["title"] = frontMatter.Title;
        }
        if (!string.IsNullOrWhiteSpace(frontMatter.Date))
        {
            customFields["date"] = frontMatter.Date;
        }
        if (!string.IsNullOrWhiteSpace(frontMatter.Author))
        {
            customFields["author"] = frontMatter.Author;
        }
        if (frontMatter.Tags != null && frontMatter.Tags.Any())
        {
            customFields["tags"] = frontMatter.Tags;
        }
        if (!string.IsNullOrWhiteSpace(frontMatter.Category))
        {
            customFields["category"] = frontMatter.Category;
        }
        if (!string.IsNullOrWhiteSpace(frontMatter.Description))
        {
            customFields["description"] = frontMatter.Description;
        }
        if (!string.IsNullOrWhiteSpace(frontMatter.Slug))
        {
            customFields["slug"] = frontMatter.Slug;
        }

        var item = new ContentItem
        {
            Slug = slug,
            CollectionName = collection.Name,
            FilePath = filePath,
            Title = frontMatter.Title,
            Date = parsedDate,
            Author = frontMatter.Author,
            Tags = frontMatter.Tags ?? new List<string>(),
            Category = frontMatter.Category,
            Description = frontMatter.Description,
            CustomFields = customFields,
            MarkdownContent = markdownContent,
            HtmlContent = htmlContent,
            LastModified = fileInfo.LastWriteTimeUtc,
            FileSizeBytes = fileInfo.Length,
            HasParsingErrors = hasErrors,
            ParsingWarnings = warnings,
            IsValid = true // Will be validated if needed
        };

        return item;
    }

    private static string? GetSlugFromFrontMatter(FrontMatter frontMatter)
    {
        return !string.IsNullOrWhiteSpace(frontMatter.Slug) ? frontMatter.Slug : null;
    }

    private static string ExtractMarkdownContent(string fullContent)
    {
        var lines = fullContent.Split('\n');

        if (lines.Length < 3 || !lines[0].Trim().Equals("---"))
        {
            return fullContent;
        }

        var endIndex = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim().Equals("---"))
            {
                endIndex = i;
                break;
            }
        }

        if (endIndex == -1)
        {
            return fullContent;
        }

        return string.Join('\n', lines[(endIndex + 1)..]).TrimStart();
    }
}
