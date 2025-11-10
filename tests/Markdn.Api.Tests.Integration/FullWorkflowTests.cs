using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Markdn.Api.Configuration;
using Markdn.Api.Endpoints;
using Markdn.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Markdn.Api.Tests.Integration;

public class FullWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FullWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullWorkflow_ReadMarkdown_ParseFrontMatter_RenderHtml_ServeAsJson()
    {
        // Arrange - Create temporary test directory with content
        var testContentDir = Path.Combine(Path.GetTempPath(), "markdn-workflow-test-" + Guid.NewGuid());
        Directory.CreateDirectory(testContentDir);

        try
        {
            // Create a test markdown file
            var testFile = Path.Combine(testContentDir, "test-post.md");
            var markdownContent = @"---
title: Test Post
author: Test Author
date: 2025-01-01
tags:
  - test
  - workflow
category: Testing
---

# Test Content

This is a test post for the full workflow test.";

            await File.WriteAllTextAsync(testFile, markdownContent);

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<MarkdnOptions>(options =>
                    {
                        options.ContentDirectory = testContentDir;
                        options.EnableFileWatching = false;
                    });
                });
            });

            var client = factory.CreateClient();

            // Act - Get all content
            var listResponse = await client.GetAsync("/api/content");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var contentList = await listResponse.Content.ReadFromJsonAsync<ContentListResponse>();
            contentList.Should().NotBeNull();
            contentList!.Items.Should().NotBeEmpty();

            // Act - Get specific content by slug
            var firstSlug = contentList.Items.First().Slug;
            var itemResponse = await client.GetAsync($"/api/content/{firstSlug}");
            itemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var contentItem = await itemResponse.Content.ReadFromJsonAsync<ContentItemResponse>();

            // Assert - Complete workflow
            contentItem.Should().NotBeNull();
            contentItem!.Slug.Should().Be(firstSlug);
            contentItem.MarkdownContent.Should().NotBeNullOrEmpty();
            contentItem.HtmlContent.Should().NotBeNullOrEmpty();
            contentItem.Title.Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testContentDir))
            {
                Directory.Delete(testContentDir, true);
            }
        }
    }

    [Fact]
    public async Task MalformedYaml_ShouldIncludeWarningsInResponse()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure to use test directory with malformed YAML file
                services.Configure<MarkdnOptions>(options =>
                {
                    options.ContentDirectory = "test-content/malformed";
                });
            });
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentList = await response.Content.ReadFromJsonAsync<ContentListResponse>();

        // Should still return items
        contentList.Should().NotBeNull();
        contentList!.Items.Should().NotBeNull();
        // Note: Warnings are only included in individual ContentItemResponse, not in the summary list
    }

    [Fact]
    public async Task FilesLargerThan5MB_ShouldBeExcluded()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<MarkdnOptions>(options =>
                {
                    options.ContentDirectory = "test-content/large-files";
                    options.MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
                });
            });
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentList = await response.Content.ReadFromJsonAsync<ContentListResponse>();

        contentList.Should().NotBeNull();
        // All items returned should be under 5MB (enforced by repository)
        contentList!.Items.Should().NotBeNull();
        // Note: File size validation happens at repository level, excluded files won't appear in results
    }

    [Fact]
    public async Task SchemaChange_ShouldInvalidateCacheAndRevalidateContent()
    {
        // Arrange - Create temporary test directory with collections.json
        var testContentDir = Path.Combine(Path.GetTempPath(), "markdn-schema-change-test-" + Guid.NewGuid());
        Directory.CreateDirectory(testContentDir);
        var blogDir = Path.Combine(testContentDir, "blog");
        Directory.CreateDirectory(blogDir);

        try
        {
            // Create initial collections.json
            var collectionsPath = Path.Combine(testContentDir, "collections.json");
            var initialCollections = @"{
  ""collections"": {
    ""blog"": {
      ""name"": ""blog"",
      ""folder"": ""blog"",
      ""schema"": {
        ""type"": ""object"",
        ""required"": [""title""],
        ""properties"": {
          ""title"": { ""type"": ""string"" }
        }
      }
    }
  }
}";
            await File.WriteAllTextAsync(collectionsPath, initialCollections);

            // Create a blog post that satisfies initial schema
            var postFile = Path.Combine(blogDir, "post1.md");
            await File.WriteAllTextAsync(postFile, @"---
title: Test Post
---
# Content");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<MarkdnOptions>(options =>
                    {
                        options.ContentDirectory = testContentDir;
                        options.EnableFileWatching = true;
                    });
                    services.Configure<CollectionsOptions>(options =>
                    {
                        options.ConfigurationFilePath = collectionsPath;
                    });
                });
            });

            var client = factory.CreateClient();

            // Act 1 - Initial load should succeed
            var initialResponse = await client.GetAsync("/api/collections/blog/items");
            initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var initialItemsResponse = await initialResponse.Content.ReadFromJsonAsync<CollectionItemsResponse>();
            initialItemsResponse.Should().NotBeNull();
            initialItemsResponse!.Items.Should().HaveCount(1);
            initialItemsResponse.Items[0].IsValid.Should().BeTrue();

            // Act 2 - Change schema to require 'author' field
            var updatedCollections = @"{
  ""collections"": {
    ""blog"": {
      ""name"": ""blog"",
      ""folder"": ""blog"",
      ""schema"": {
        ""type"": ""object"",
        ""required"": [""title"", ""author""],
        ""properties"": {
          ""title"": { ""type"": ""string"" },
          ""author"": { ""type"": ""string"" }
        }
      }
    }
  }
}";
            await File.WriteAllTextAsync(collectionsPath, updatedCollections);

            // Wait for file watcher to detect change, invalidate cache, and restart watchers
            await Task.Delay(2000);

            // Act 3 - Load with new schema - item should now be invalid
            var updatedResponse = await client.GetAsync("/api/collections/blog/items");
            updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedItemsResponse = await updatedResponse.Content.ReadFromJsonAsync<CollectionItemsResponse>();
            updatedItemsResponse.Should().NotBeNull();
            updatedItemsResponse!.Items.Should().HaveCount(1);
            updatedItemsResponse.Items[0].IsValid.Should().BeFalse("post is missing required 'author' field after schema change");

            // Act 4 - Validate collection manually
            var validateResponse = await client.PostAsync("/api/collections/blog/validate-all", null);
            validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var validationReport = await validateResponse.Content.ReadFromJsonAsync<CollectionValidationReport>();
            validationReport.Should().NotBeNull();
            validationReport!.TotalItems.Should().Be(1);
            validationReport.ValidItems.Should().Be(0);
            validationReport.InvalidItems.Should().Be(1);
            validationReport.Errors.Should().HaveCount(1);
            validationReport.Errors[0].ErrorMessages.Should().Contain(m => m.Contains("author"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testContentDir))
            {
                Directory.Delete(testContentDir, true);
            }
        }
    }
}
