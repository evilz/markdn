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

    // User Story 2: Filtering tests

    [Fact]
    public async Task GetAllAsync_WithTagFilter_ShouldReturnMatchingItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Tags = new List<string> { "tutorial", "csharp" } },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Tags = new List<string> { "news" } },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Tags = new List<string> { "tutorial", "dotnet" } }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest { Tag = "tutorial" };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item => item.Tags.Should().Contain("tutorial"));
    }

    [Fact]
    public async Task GetAllAsync_WithCategoryFilter_ShouldReturnMatchingItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Category = "blog" },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Category = "tutorial" },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Category = "blog" }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest { Category = "blog" };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item => item.Category.Should().Be("blog"));
    }

    [Fact]
    public async Task GetAllAsync_WithDateRangeFilter_ShouldReturnMatchingItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Date = new DateTime(2025, 1, 15) },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Date = new DateTime(2025, 6, 20) },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Date = new DateTime(2025, 12, 10) }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest 
        { 
            DateFrom = new DateTime(2025, 6, 1),
            DateTo = new DateTime(2025, 12, 31)
        };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item => 
        {
            item.Date.Should().NotBeNull();
            item.Date!.Value.Should().BeOnOrAfter(new DateTime(2025, 6, 1));
            item.Date!.Value.Should().BeOnOrBefore(new DateTime(2025, 12, 31));
        });
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleFilters_ShouldReturnMatchingItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Category = "blog", Tags = new List<string> { "tutorial" } },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Category = "blog", Tags = new List<string> { "news" } },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Category = "tutorial", Tags = new List<string> { "tutorial" } }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest 
        { 
            Tag = "tutorial",
            Category = "blog"
        };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Slug.Should().Be("post-1");
    }

    [Fact]
    public async Task GetAllAsync_WithSortByDateAscending_ShouldReturnSortedItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Date = new DateTime(2025, 6, 15) },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Date = new DateTime(2025, 1, 10) },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Date = new DateTime(2025, 12, 5) }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest 
        { 
            SortBy = "date",
            SortOrder = "asc"
        };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Date.Should().Be(new DateTime(2025, 1, 10));
        result.Items[1].Date.Should().Be(new DateTime(2025, 6, 15));
        result.Items[2].Date.Should().Be(new DateTime(2025, 12, 5));
    }

    [Fact]
    public async Task GetAllAsync_WithSortByDateDescending_ShouldReturnSortedItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Date = new DateTime(2025, 6, 15) },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Date = new DateTime(2025, 1, 10) },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Date = new DateTime(2025, 12, 5) }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest 
        { 
            SortBy = "date",
            SortOrder = "desc"
        };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Date.Should().Be(new DateTime(2025, 12, 5));
        result.Items[1].Date.Should().Be(new DateTime(2025, 6, 15));
        result.Items[2].Date.Should().Be(new DateTime(2025, 1, 10));
    }

    [Fact]
    public async Task GetAllAsync_WithSortByTitle_ShouldReturnSortedItems()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Title = "Zebra" },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Title = "Apple" },
                    new ContentItem { Slug = "post-3", FilePath = "/path/post-3.md", MarkdownContent = "# Post 3", Title = "Banana" }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest 
        { 
            SortBy = "title",
            SortOrder = "asc"
        };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Title.Should().Be("Apple");
        result.Items[1].Title.Should().Be("Banana");
        result.Items[2].Title.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetAllAsync_WithNoMatchingFilters_ShouldReturnEmptyCollection()
    {
        // Arrange
        var mockRepository = new Mock<IContentRepository>();
        mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentCollection
            {
                Items = new List<ContentItem>
                {
                    new ContentItem { Slug = "post-1", FilePath = "/path/post-1.md", MarkdownContent = "# Post 1", Tags = new List<string> { "tutorial" } },
                    new ContentItem { Slug = "post-2", FilePath = "/path/post-2.md", MarkdownContent = "# Post 2", Tags = new List<string> { "news" } }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50
            });

        var service = new ContentService(mockRepository.Object);
        var query = new ContentQueryRequest { Tag = "nonexistent" };

        // Act
        var result = await service.GetAllAsync(query, 1, 50, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
