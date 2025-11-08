using FluentAssertions;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Moq;
using Xunit;

namespace Markdn.Api.Tests.Unit.Services;

public class ContentServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnContentCollection()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "test-1", FilePath = "/path/test-1.md", MarkdownContent = "# Test 1" },
                    new ContentItem { Slug = "test-2", FilePath = "/path/test-2.md", MarkdownContent = "# Test 2" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);

        // Act
        var result = await service.GetAllAsync(1, 50, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetBySlugAsync_WithExistingSlug_ShouldReturnContentItem()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        var expectedItem = new ContentItem 
        { 
            Slug = "test-slug", 
            FilePath = "/path/test-slug.md", 
            MarkdownContent = "# Test" 
        };
        
        mockRepository.Setup(r => r.GetBySlugAsync("test-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItem);

        var service = new ContentService(mockRepository.Object);

        // Act
        var result = await service.GetBySlugAsync("test-slug", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("test-slug");
    }

    [Fact]
    public async Task GetBySlugAsync_WithNonExistentSlug_ShouldReturnNull()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetBySlugAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentItem?)null);

        var service = new ContentService(mockRepository.Object);

        // Act
        var result = await service.GetBySlugAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
