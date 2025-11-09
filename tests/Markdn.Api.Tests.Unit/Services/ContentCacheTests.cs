using FluentAssertions;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Markdn.Api.Tests.Unit.Services;

public class ContentCacheTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<IContentRepository> _repositoryMock;
    private readonly Mock<ILogger<ContentCache>> _loggerMock;
    private readonly ContentCache _contentCache;

    public ContentCacheTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _repositoryMock = new Mock<IContentRepository>();
        _loggerMock = new Mock<ILogger<ContentCache>>();
        _contentCache = new ContentCache(_cache, _repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ContentCache_ShouldInvalidateOnFileChange()
    {
        // Arrange
        var item = new ContentItem
        {
            Slug = "test",
            Title = "Test",
            FilePath = "test.md",
            MarkdownContent = "# Test",
            HtmlContent = "<h1>Test</h1>"
        };
        _contentCache.Set("test", item);

        // Act
        _contentCache.Invalidate("test");
        var result = _contentCache.Get("test");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ContentCache_ShouldRefreshAfterInvalidation()
    {
        // Arrange
        var originalItem = new ContentItem
        {
            Slug = "test",
            Title = "Original",
            FilePath = "test.md",
            MarkdownContent = "# Original",
            HtmlContent = "<h1>Original</h1>"
        };
        var updatedItem = new ContentItem
        {
            Slug = "test",
            Title = "Updated",
            FilePath = "test.md",
            MarkdownContent = "# Updated",
            HtmlContent = "<h1>Updated</h1>"
        };

        _contentCache.Set("test", originalItem);
        _repositoryMock.Setup(r => r.GetBySlugAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedItem);

        // Act
        _contentCache.Invalidate("test");
        await _contentCache.RefreshAsync("test");
        var result = _contentCache.Get("test");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.HtmlContent.Should().Contain("Updated");
    }

    [Fact]
    public void ContentCache_ShouldReturnNullForMissingKey()
    {
        // Act
        var result = _contentCache.Get("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ContentCache_ShouldStoreAndRetrieveItems()
    {
        // Arrange
        var item = new ContentItem
        {
            Slug = "test",
            Title = "Test",
            FilePath = "test.md",
            MarkdownContent = "# Test",
            HtmlContent = "<h1>Test</h1>"
        };

        // Act
        _contentCache.Set("test", item);
        var result = _contentCache.Get("test");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("test");
        result.Title.Should().Be("Test");
        result.HtmlContent.Should().Contain("Test");
    }
}
