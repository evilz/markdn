using FluentAssertions;
using Markdn.Api.Services;
using Markdn.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Markdn.Api.Tests.Unit.Services;

/// <summary>
/// Unit tests for CollectionLoader service.
/// Tests collection configuration loading and schema parsing.
/// </summary>
public class CollectionLoaderTests
{
    private readonly Mock<ILogger<CollectionLoader>> _loggerMock;
    private readonly Mock<IOptions<CollectionsOptions>> _optionsMock;
    private readonly IMemoryCache _cache;

    public CollectionLoaderTests()
    {
        _loggerMock = new Mock<ILogger<CollectionLoader>>();
    _optionsMock = new Mock<IOptions<CollectionsOptions>>();
    _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        _optionsMock.Setup(o => o.Value).Returns(new CollectionsOptions
        {
            ConfigurationFilePath = "content/collections.json"
        });
    }

    [Fact]
    public async Task LoadCollectionsAsync_WithValidConfigFile_ShouldLoadCollections()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var jsonContent = @"{
            ""contentRootPath"": ""content"",
            ""collections"": {
                ""blog"": {
                    ""folder"": ""blog"",
                    ""schema"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""title"": { ""type"": ""string"" },
                            ""author"": { ""type"": ""string"" }
                        },
                        ""required"": [""title""]
                    }
                }
            }
        }";
        await File.WriteAllTextAsync(tempFile, jsonContent);

        _optionsMock.Setup(o => o.Value).Returns(new CollectionsOptions
        {
            ConfigurationFilePath = tempFile
        });

    var loader = new CollectionLoader(_optionsMock.Object, _cache, _loggerMock.Object);

        // Act
        var collections = await loader.LoadCollectionsAsync();

        // Assert
        collections.Should().NotBeNull();
        collections.Should().ContainKey("blog");
        collections["blog"].Name.Should().Be("blog");
        collections["blog"].Schema.Should().NotBeNull();
        collections["blog"].Schema.Properties.Should().ContainKey("title");

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task LoadCollectionsAsync_WithValidConfigFile_ShouldParseJsonSchema()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var jsonContent = @"{
            ""contentRootPath"": ""content"",
            ""collections"": {
                ""docs"": {
                    ""folder"": ""docs"",
                    ""schema"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""title"": { 
                                ""type"": ""string"",
                                ""minLength"": 1,
                                ""maxLength"": 100
                            },
                            ""category"": { 
                                ""type"": ""string"",
                                ""enum"": [""tutorial"", ""guide"", ""reference""]
                            }
                        },
                        ""required"": [""title"", ""category""]
                    }
                }
            }
        }";
        await File.WriteAllTextAsync(tempFile, jsonContent);

        _optionsMock.Setup(o => o.Value).Returns(new CollectionsOptions
        {
            ConfigurationFilePath = tempFile
        });

    var loader = new CollectionLoader(_optionsMock.Object, _cache, _loggerMock.Object);

        // Act
        var collections = await loader.LoadCollectionsAsync();

        // Assert
        collections.Should().ContainKey("docs");
        var docsCollection = collections["docs"];
        docsCollection.Schema.Required.Should().Contain("title");
        docsCollection.Schema.Required.Should().Contain("category");
        docsCollection.Schema.Properties["title"].MinLength.Should().Be(1);
        docsCollection.Schema.Properties["title"].MaxLength.Should().Be(100);
        docsCollection.Schema.Properties["category"].Enum.Should().BeEquivalentTo("tutorial", "guide", "reference");

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task LoadCollectionsAsync_WithMissingConfigFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns(new CollectionsOptions
        {
            ConfigurationFilePath = "nonexistent/collections.json"
        });

    var loader = new CollectionLoader(_optionsMock.Object, _cache, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await loader.LoadCollectionsAsync());
    }

    [Fact]
    public async Task LoadCollectionsAsync_WithInvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "{invalid json content");

        _optionsMock.Setup(o => o.Value).Returns(new CollectionsOptions
        {
            ConfigurationFilePath = tempFile
        });

    var loader = new CollectionLoader(_optionsMock.Object, _cache, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await loader.LoadCollectionsAsync());

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task LoadCollectionsAsync_WithEmptyCollections_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var jsonContent = @"{
            ""contentRootPath"": ""content"",
            ""collections"": {}
        }";
        await File.WriteAllTextAsync(tempFile, jsonContent);

        _optionsMock.Setup(o => o.Value).Returns(new CollectionsOptions
        {
            ConfigurationFilePath = tempFile
        });

    var loader = new CollectionLoader(_optionsMock.Object, _cache, _loggerMock.Object);

        // Act
        var collections = await loader.LoadCollectionsAsync();

        // Assert
        collections.Should().NotBeNull();
        collections.Should().BeEmpty();

        // Cleanup
        File.Delete(tempFile);
    }
}
