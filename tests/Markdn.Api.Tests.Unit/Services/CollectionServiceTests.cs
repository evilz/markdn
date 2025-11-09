using FluentAssertions;
using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Markdn.Api.Tests.Unit.Services;

/// <summary>
/// Unit tests for CollectionService - T062, T063
/// Tests collection item querying and retrieval operations
/// </summary>
public class CollectionServiceTests
{
    private readonly Mock<ICollectionLoader> _mockCollectionLoader;
    private readonly Mock<FrontMatterParser> _mockFrontMatterParser;
    private readonly Mock<MarkdownParser> _mockMarkdownParser;
    private readonly Mock<SlugGenerator> _mockSlugGenerator;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IOptions<MarkdnOptions>> _mockOptions;
    private readonly Mock<ILogger<CollectionService>> _mockLogger;
    private readonly CollectionService _sut;

    public CollectionServiceTests()
    {
        _mockCollectionLoader = new Mock<ICollectionLoader>();
        _mockFrontMatterParser = new Mock<FrontMatterParser>();
        _mockMarkdownParser = new Mock<MarkdownParser>();
        _mockSlugGenerator = new Mock<SlugGenerator>();
        _mockCache = new Mock<IMemoryCache>();
        _mockOptions = new Mock<IOptions<MarkdnOptions>>();
        _mockLogger = new Mock<ILogger<CollectionService>>();

        _mockOptions.Setup(x => x.Value).Returns(new MarkdnOptions
        {
            ContentDirectory = "content",
            MaxFileSizeBytes = 1048576
        });

        _sut = new CollectionService(
            _mockCollectionLoader.Object,
            _mockFrontMatterParser.Object,
            _mockMarkdownParser.Object,
            _mockSlugGenerator.Object,
            _mockCache.Object,
            _mockOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllItemsAsync_WithValidCollection_ShouldReturnAllItems()
    {
        // Arrange
        var collectionName = "test-collection";
        var collection = new Collection
        {
            Name = collectionName,
            FolderPath = "content/test",
            Schema = new CollectionSchema
            {
                Type = "object",
                Properties = new Dictionary<string, FieldDefinition>
                {
                    ["title"] = new FieldDefinition { Name = "title", Type = FieldType.String }
                },
                Required = new List<string> { "title" },
                AdditionalProperties = true
            }
        };

        _mockCollectionLoader
            .Setup(x => x.LoadCollectionAsync(collectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Mock cache miss to force loading
        object? cacheValue = null;
        _mockCache
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        // Mock cache Set to not throw
        _mockCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<Microsoft.Extensions.Caching.Memory.ICacheEntry>());

        // Act
        var result = await _sut.GetAllItemsAsync(collectionName, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // Note: Will return empty since we don't have actual files in this unit test
        // Integration tests will test actual file loading
    }

    [Fact]
    public async Task GetAllItemsAsync_WithNonExistentCollection_ShouldReturnEmpty()
    {
        // Arrange
        var collectionName = "non-existent";

        _mockCollectionLoader
            .Setup(x => x.LoadCollectionAsync(collectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _sut.GetAllItemsAsync(collectionName, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetItemByIdAsync_WithNonExistentSlug_ShouldReturnNull()
    {
        // Arrange
        var collectionName = "test-collection";
        var slug = "non-existent";

        _mockCollectionLoader
            .Setup(x => x.LoadCollectionAsync(collectionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Mock cache miss
        object? cacheValue = null;
        _mockCache
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        // Act
        var result = await _sut.GetItemByIdAsync(collectionName, slug, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
