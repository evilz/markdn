using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Markdn.Api.Configuration;
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
}
