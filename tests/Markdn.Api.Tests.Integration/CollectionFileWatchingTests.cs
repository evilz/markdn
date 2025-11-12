using FluentAssertions;
using Markdn.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Markdn.Api.Tests.Integration;

public class CollectionFileWatchingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testContentDir;

    public CollectionFileWatchingTests(WebApplicationFactory<Program> factory)
    {
        _testContentDir = Path.Combine(Path.GetTempPath(), "markdn-collection-watch-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testContentDir);

        // Create collections.json
        var collectionsPath = Path.Combine(_testContentDir, "collections.json");
        var collectionsConfig = new
        {
            collections = new Dictionary<string, object>
            {
                ["blog"] = new
                {
                    name = "blog",
                    folderPath = "blog",
                    schema = new
                    {
                        type = "object",
                        required = new[] { "title", "author" },
                        properties = new
                        {
                            title = new { type = "string" },
                            author = new { type = "string" },
                            date = new { type = "string", format = "date" }
                        }
                    }
                }
            }
        };
        File.WriteAllText(collectionsPath, JsonSerializer.Serialize(collectionsConfig, new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        // Create blog folder
        Directory.CreateDirectory(Path.Combine(_testContentDir, "blog"));

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Markdn:ContentDirectory"] = _testContentDir,
                    ["Markdn:EnableFileWatching"] = "true",
                    ["Collections:ConfigurationFilePath"] = collectionsPath // Use full path
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CollectionFileCreation_ShouldInvalidateCacheAndReloadContent()
    {
        // Arrange - Start with one file
        var blogDir = Path.Combine(_testContentDir, "blog");
        var file1 = Path.Combine(blogDir, "post1.md");
        await File.WriteAllTextAsync(file1, @"---
title: First Post
author: John Doe
date: 2024-01-01
---
# First Content");

        await Task.Delay(1000); // Wait for initial load

    // Load once to populate cache
    var initialResponse = await _client.GetAsync("/api/collections/blog/items");
    var initialWrapper = await initialResponse.Content.ReadFromJsonAsync<CollectionItemsResponse>();
    initialWrapper.Should().NotBeNull();
    initialWrapper!.Items.Count.Should().Be(1);

        // Act - Create a new file
        var file2 = Path.Combine(blogDir, "post2.md");
        await File.WriteAllTextAsync(file2, @"---
title: Second Post
author: Jane Smith
date: 2024-01-02
---
# Second Content");

        await Task.Delay(1000); // Wait for file watcher debounce and cache invalidation

    var response = await _client.GetAsync("/api/collections/blog/items");
    var wrapper = await response.Content.ReadFromJsonAsync<CollectionItemsResponse>();

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    wrapper.Should().NotBeNull();
    wrapper!.Items.Count.Should().Be(2);
    wrapper.Items.Should().Contain(i => i.Title == "Second Post");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }

    [Fact]
    public async Task CollectionFileModification_ShouldInvalidateCacheAndReloadContent()
    {
        // Arrange
        var blogDir = Path.Combine(_testContentDir, "blog");
        var testFile = Path.Combine(blogDir, "test-post.md");
        await File.WriteAllTextAsync(testFile, @"---
title: Original Title
author: John Doe
date: 2024-01-01
---
# Original Content");

        await Task.Delay(1000); // Wait for initial load

    // Load once to populate cache
    var initialResponse = await _client.GetAsync("/api/collections/blog/items");
    initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Modify the file
        await File.WriteAllTextAsync(testFile, @"---
title: Updated Title
author: John Doe
date: 2024-01-01
---
# Updated Content");

        await Task.Delay(1000); // Wait for file watcher debounce and cache invalidation

    var response = await _client.GetAsync("/api/collections/blog/items");
    var wrapper = await response.Content.ReadFromJsonAsync<CollectionItemsResponse>();

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    wrapper.Should().NotBeNull();
    wrapper!.Items.Should().ContainSingle();
    var item = wrapper.Items[0];
    item.Title.Should().Be("Updated Title");
    item.HtmlContent.Should().Contain("Updated Content");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }

    [Fact]
    public async Task CollectionFileDeletion_ShouldInvalidateCacheAndRemoveContent()
    {
        // Arrange - Start with two files
        var blogDir = Path.Combine(_testContentDir, "blog");
        var file1 = Path.Combine(blogDir, "post1.md");
        var file2 = Path.Combine(blogDir, "post2.md");
        
        await File.WriteAllTextAsync(file1, @"---
title: First Post
author: John Doe
date: 2024-01-01
---
# First Content");

        await File.WriteAllTextAsync(file2, @"---
title: Second Post
author: Jane Smith
date: 2024-01-02
---
# Second Content");

        await Task.Delay(1000); // Wait for initial load

    // Load once to populate cache
    var initialResponse = await _client.GetAsync("/api/collections/blog/items");
    var initialWrapper = await initialResponse.Content.ReadFromJsonAsync<CollectionItemsResponse>();
    initialWrapper.Should().NotBeNull();
    initialWrapper!.Items.Count.Should().Be(2);

        // Act - Delete one file
        File.Delete(file2);

        await Task.Delay(1000); // Wait for file watcher debounce and cache invalidation

    var response = await _client.GetAsync("/api/collections/blog/items");
    var wrapper = await response.Content.ReadFromJsonAsync<CollectionItemsResponse>();

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    wrapper.Should().NotBeNull();
    wrapper!.Items.Should().ContainSingle();
    wrapper.Items[0].Title.Should().Be("First Post");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }
}
