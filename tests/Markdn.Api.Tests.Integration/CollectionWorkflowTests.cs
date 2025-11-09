using FluentAssertions;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Markdn.Api.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace Markdn.Api.Tests.Integration;

/// <summary>
/// Integration tests for complete collection workflows.
/// Tests end-to-end collection loading, validation, and retrieval scenarios.
/// </summary>
public class CollectionWorkflowTests
{
    [Fact]
    public async Task LoadAndRetrieveCollectionMetadata_ShouldWorkEndToEnd()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var collectionsFile = Path.Combine(tempDir, "collections.json");
        var jsonContent = @"{
            ""contentRootPath"": ""content"",
            ""collections"": {
                ""blog"": {
                    ""folder"": ""blog"",
                    ""schema"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""title"": { 
                                ""type"": ""string"",
                                ""minLength"": 1
                            },
                            ""author"": { ""type"": ""string"" },
                            ""publishDate"": { 
                                ""type"": ""string"",
                                ""format"": ""date""
                            }
                        },
                        ""required"": [""title"", ""publishDate""]
                    }
                },
                ""docs"": {
                    ""folder"": ""docs"",
                    ""schema"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""title"": { ""type"": ""string"" },
                            ""category"": { ""type"": ""string"" }
                        },
                        ""required"": [""title""]
                    }
                }
            }
        }";
        await File.WriteAllTextAsync(collectionsFile, jsonContent);

        var options = Options.Create(new CollectionsOptions
        {
            ConfigurationFilePath = collectionsFile
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<CollectionLoader>>().Object;
        var loader = new CollectionLoader(options, cache, logger);

        // Act
        var collections = await loader.LoadCollectionsAsync();

        // Assert - Verify collections are loaded
        collections.Should().NotBeNull();
        collections.Should().HaveCount(2);
        collections.Should().ContainKey("blog");
        collections.Should().ContainKey("docs");

        // Assert - Verify blog collection metadata
        var blogCollection = collections["blog"];
        blogCollection.Name.Should().Be("blog");
        blogCollection.FolderPath.Should().Be("blog");
        blogCollection.Schema.Should().NotBeNull();
        blogCollection.Schema.Properties.Should().HaveCount(3);
        blogCollection.Schema.Required.Should().Contain("title");
        blogCollection.Schema.Required.Should().Contain("publishDate");

        // Assert - Verify docs collection metadata
        var docsCollection = collections["docs"];
        docsCollection.Name.Should().Be("docs");
        docsCollection.FolderPath.Should().Be("docs");
        docsCollection.Schema.Required.Should().Contain("title");
        docsCollection.Schema.Required.Should().HaveCount(1);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task LoadCollections_WithNestedSchemaProperties_ShouldParseCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var collectionsFile = Path.Combine(tempDir, "collections.json");
        var jsonContent = @"{
            ""contentRootPath"": ""content"",
            ""collections"": {
                ""articles"": {
                    ""folder"": ""articles"",
                    ""schema"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""title"": { ""type"": ""string"" },
                            ""tags"": {
                                ""type"": ""array"",
                                ""items"": { ""type"": ""string"" }
                            },
                            ""metadata"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""views"": { ""type"": ""number"" },
                                    ""likes"": { ""type"": ""number"" }
                                }
                            }
                        },
                        ""required"": [""title""]
                    }
                }
            }
        }";
        await File.WriteAllTextAsync(collectionsFile, jsonContent);

        var options = Options.Create(new CollectionsOptions
        {
            ConfigurationFilePath = collectionsFile
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<CollectionLoader>>().Object;
        var loader = new CollectionLoader(options, cache, logger);

        // Act
        var collections = await loader.LoadCollectionsAsync();

        // Assert
        collections.Should().ContainKey("articles");
        var articlesCollection = collections["articles"];
        articlesCollection.Schema.Properties.Should().ContainKey("tags");
        articlesCollection.Schema.Properties.Should().ContainKey("metadata");

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}
