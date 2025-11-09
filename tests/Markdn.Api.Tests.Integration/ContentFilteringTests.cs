using FluentAssertions;
using Markdn.Api.FileSystem;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Markdn.Api.Tests.Integration;

/// <summary>
/// Integration tests for content filtering functionality (User Story 2)
/// </summary>
public class ContentFilteringTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly IContentRepository _repository;
    private readonly ContentService _service;

    public ContentFilteringTests()
    {
        // Setup temp directory with test content
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"markdn_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        var options = Options.Create(new Configuration.MarkdnOptions
        {
            ContentDirectory = _tempDirectory,
            MaxFileSizeBytes = 5 * 1024 * 1024
        });

        var frontMatterParser = new FrontMatterParser();
        var markdownParser = new MarkdownParser();
        var slugGenerator = new SlugGenerator();

        _repository = new FileSystemContentRepository(
            options,
            frontMatterParser,
            markdownParser,
            slugGenerator
        );

        _service = new ContentService(
            _repository, 
            null!, 
            new LoggerFactory().CreateLogger<ContentService>()
        );
    }

    [Fact]
    public async Task GetAllAsync_WithComplexMultiFilterQuery_ShouldReturnMatchingItems()
    {
        // Arrange: Create test files with various metadata
        await CreateTestFile("post1.md", new
        {
            title = "Tutorial Post",
            category = "blog",
            tags = new[] { "tutorial", "csharp" },
            date = "2025-06-01"
        }, "Content for post 1");

        await CreateTestFile("post2.md", new
        {
            title = "Guide Post",
            category = "documentation",
            tags = new[] { "tutorial", "dotnet" },
            date = "2025-07-01"
        }, "Content for post 2");

        await CreateTestFile("post3.md", new
        {
            title = "News Post",
            category = "blog",
            tags = new[] { "news" },
            date = "2025-08-01"
        }, "Content for post 3");

        var query = new ContentQueryRequest
        {
            Tag = "tutorial",
            Category = "blog",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(query, query.Page, query.PageSize, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Slug.Should().Be("post1");
    }

    [Fact]
    public async Task GetAllAsync_WithPaginationAndFilters_ShouldReturnCorrectPage()
    {
        // Arrange: Create multiple test files
        for (int i = 1; i <= 15; i++)
        {
            await CreateTestFile($"post{i}.md", new
            {
                title = $"Post {i}",
                category = "blog",
                tags = new[] { "tutorial" },
                date = $"2025-{i:D2}-01"
            }, $"Content for post {i}");
        }

        var query = new ContentQueryRequest
        {
            Category = "blog",
            Page = 2,
            PageSize = 5,
            SortBy = "date",
            SortOrder = "asc"
        };

        // Act
        var result = await _service.GetAllAsync(query, query.Page, query.PageSize, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.Page.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.TotalCount.Should().Be(15);
    }

    private async Task CreateTestFile(string filename, object frontMatter, string content)
    {
        var filePath = Path.Combine(_tempDirectory, filename);
        
        var yamlContent = System.Text.Json.JsonSerializer.Serialize(frontMatter, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }).Replace("{", "").Replace("}", "").Replace("\"", "").Replace(",", "");

        var fileContent = $@"---
{yamlContent}
---

{content}";

        await File.WriteAllTextAsync(filePath, fileContent);
    }

    public void Dispose()
    {
        // Cleanup temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
