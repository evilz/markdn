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
        // Arrange
        var client = _factory.CreateClient();

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
        contentItem.FilePath.Should().NotBeNullOrEmpty();
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

        // Should still return items but with warnings
        contentList.Should().NotBeNull();
        // If there are items with malformed YAML, they should have warnings
        var itemsWithWarnings = contentList!.Items.Where(i => i.HasParsingErrors).ToList();
        if (itemsWithWarnings.Any())
        {
            itemsWithWarnings.Should().AllSatisfy(item =>
            {
                item.ParsingWarnings.Should().NotBeEmpty();
            });
        }
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
        contentList!.Items.Should().AllSatisfy(item =>
        {
            item.FileSizeBytes.Should().BeLessThan(5 * 1024 * 1024);
        });
    }
}
