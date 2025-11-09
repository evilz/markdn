using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.Extensions.Options;

namespace Markdn.Api.FileSystem;

/// <summary>
/// File system-based implementation of content repository with security validation
/// </summary>
public class FileSystemContentRepository : IContentRepository
{
    private readonly MarkdnOptions _options;
    private readonly FrontMatterParser _frontMatterParser;
    private readonly MarkdownParser _markdownParser;
    private readonly SlugGenerator _slugGenerator;

    /// <summary>
    /// Initializes a new instance of the FileSystemContentRepository
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="frontMatterParser">Front-matter parser service</param>
    /// <param name="markdownParser">Markdown parser service</param>
    /// <param name="slugGenerator">Slug generator service</param>
    public FileSystemContentRepository(
        IOptions<MarkdnOptions> options,
        FrontMatterParser frontMatterParser,
        MarkdownParser markdownParser,
        SlugGenerator slugGenerator)
    {
        _options = options.Value;
        _frontMatterParser = frontMatterParser;
        _markdownParser = markdownParser;
        _slugGenerator = slugGenerator;
    }

    /// <summary>
    /// Retrieves all content items from the file system with security validation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all valid content items</returns>
    public async Task<ContentCollection> GetAllAsync(CancellationToken cancellationToken)
    {
        var contentDirectory = GetSafeContentDirectory();

        if (!Directory.Exists(contentDirectory))
        {
            return new ContentCollection
            {
                Items = new List<ContentItem>(),
                TotalCount = 0,
                Page = 1,
                PageSize = _options.DefaultPageSize
            };
        }

        var markdownFiles = Directory.GetFiles(contentDirectory, "*.md", SearchOption.AllDirectories);
        var items = new List<ContentItem>();

        foreach (var filePath in markdownFiles)
        {
            // Security: Verify file is within content directory
            if (!IsPathSafe(filePath, contentDirectory))
            {
                continue;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);

                // Skip files larger than max size
                if (fileInfo.Length > _options.MaxFileSizeBytes)
                {
                    continue;
                }

                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var item = await ParseContentItemAsync(filePath, content, fileInfo, cancellationToken);
                items.Add(item);
            }
            catch
            {
                // Skip files that can't be read
                continue;
            }
        }

        return new ContentCollection
        {
            Items = items,
            TotalCount = items.Count,
            Page = 1,
            PageSize = _options.DefaultPageSize
        };
    }

    /// <summary>
    /// Retrieves a single content item by slug with input validation
    /// </summary>
    /// <param name="slug">Unique slug identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content item if found and valid, null otherwise</returns>
    public async Task<ContentItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        // Security: Validate slug input
        if (!IsSlugValid(slug))
        {
            return null;
        }

        var contentDirectory = Path.GetFullPath(_options.ContentDirectory);

        if (!Directory.Exists(contentDirectory))
        {
            return null;
        }

        var markdownFiles = Directory.GetFiles(contentDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var filePath in markdownFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // Skip files larger than max size
                if (fileInfo.Length > _options.MaxFileSizeBytes)
                {
                    continue;
                }

                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var item = await ParseContentItemAsync(filePath, content, fileInfo, cancellationToken);

                if (item.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private async Task<ContentItem> ParseContentItemAsync(
        string filePath,
        string content,
        FileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var hasErrors = false;

        // Parse front-matter
        FrontMatter frontMatter;
        try
        {
            frontMatter = await _frontMatterParser.ParseAsync(content, cancellationToken);
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
            htmlContent = await _markdownParser.ParseAsync(markdownContent, cancellationToken);
        }
        catch (Exception ex)
        {
            warnings.Add($"Failed to parse markdown: {ex.Message}");
            hasErrors = true;
        }

        return new ContentItem
        {
            Slug = slug,
            FilePath = filePath,
            Title = frontMatter.Title,
            Date = parsedDate,
            Author = frontMatter.Author,
            Tags = frontMatter.Tags ?? new List<string>(),
            Category = frontMatter.Category,
            Description = frontMatter.Description,
            CustomFields = frontMatter.AdditionalProperties,
            MarkdownContent = markdownContent,
            HtmlContent = htmlContent,
            LastModified = fileInfo.LastWriteTimeUtc,
            FileSizeBytes = fileInfo.Length,
            HasParsingErrors = hasErrors,
            ParsingWarnings = warnings
        };
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

    /// <summary>
    /// Gets the content directory with full path resolution and validation
    /// </summary>
    private string GetSafeContentDirectory()
    {
        return Path.GetFullPath(_options.ContentDirectory);
    }

    /// <summary>
    /// Validates that a file path is within the content directory (prevents path traversal attacks)
    /// </summary>
    private static bool IsPathSafe(string filePath, string contentDirectory)
    {
        try
        {
            var fullFilePath = Path.GetFullPath(filePath);
            var fullContentDir = Path.GetFullPath(contentDirectory);

            // Ensure the file path starts with the content directory path
            return fullFilePath.StartsWith(fullContentDir, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If any path operation fails, consider it unsafe
            return false;
        }
    }

    /// <summary>
    /// Validates slug input to prevent injection attacks and path traversal
    /// </summary>
    private static bool IsSlugValid(string slug)
    {
        // Reject null/empty
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        // Reject excessive length (max 200 chars)
        if (slug.Length > 200)
        {
            return false;
        }

        // Reject path traversal attempts
        if (slug.Contains("..") || slug.Contains('/') || slug.Contains('\\'))
        {
            return false;
        }

        // Only allow alphanumeric, hyphens, and underscores
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-zA-Z0-9\-_]+$");
    }
}

