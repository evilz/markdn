using FluentAssertions;
using Markdn.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Markdn.Api.Tests.Integration;

public class FileWatchingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testContentDir;

    public FileWatchingTests(WebApplicationFactory<Program> factory)
    {
        _testContentDir = Path.Combine(Path.GetTempPath(), "markdn-watch-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testContentDir);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Markdn:ContentDirectory"] = _testContentDir,
                    ["Markdn:EnableFileWatching"] = "true"
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FileModification_ShouldBeDetectedAndServedWithin5Seconds()
    {
        // Arrange
        var testFile = Path.Combine(_testContentDir, "test.md");
        await File.WriteAllTextAsync(testFile, @"---
title: Original Title
---
# Original Content");

        await Task.Delay(2000); // Wait for initial load

        // Act - Modify the file
        await File.WriteAllTextAsync(testFile, @"---
title: Updated Title
---
# Updated Content");

        await Task.Delay(5000); // Wait for file watcher to detect and update

        var response = await _client.GetAsync("/api/content/test");
        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Title.Should().Be("Updated Title");
        content.HtmlContent.Should().Contain("Updated Content");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }

    [Fact]
    public async Task NewFileCreation_ShouldBeDetectedAndAddedToCollection()
    {
        // Arrange - Start with empty directory
        var initialResponse = await _client.GetAsync("/api/content");
        var initialContent = await initialResponse.Content.ReadFromJsonAsync<ContentListResponse>();
        var initialCount = initialContent?.Items.Count ?? 0;

        // Act - Create a new file
        var newFile = Path.Combine(_testContentDir, "new-post.md");
        await File.WriteAllTextAsync(newFile, @"---
title: New Post
---
# New Content");

        await Task.Delay(5000); // Wait for file watcher

        var response = await _client.GetAsync("/api/content");
        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Items.Count.Should().Be(initialCount + 1);
        content.Items.Should().Contain(i => i.Slug == "new-post");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }

    [Fact]
    public async Task FileDeletion_ShouldBeDetectedAndRemovedFromCollection()
    {
        // Arrange - Create a file
        var testFile = Path.Combine(_testContentDir, "to-delete.md");
        await File.WriteAllTextAsync(testFile, @"---
title: To Delete
---
# Content");

        await Task.Delay(2000); // Wait for initial load

        var initialResponse = await _client.GetAsync("/api/content");
        var initialContent = await initialResponse.Content.ReadFromJsonAsync<ContentListResponse>();
        initialContent!.Items.Should().Contain(i => i.Slug == "to-delete");

        // Act - Delete the file
        File.Delete(testFile);
        await Task.Delay(5000); // Wait for file watcher

        var response = await _client.GetAsync("/api/content");
        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Items.Should().NotContain(i => i.Slug == "to-delete");

        // Cleanup
        Directory.Delete(_testContentDir, true);
    }
}
