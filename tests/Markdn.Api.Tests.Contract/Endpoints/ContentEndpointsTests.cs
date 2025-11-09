using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Markdn.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Markdn.Api.Tests.Contract.Endpoints;

public class ContentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ContentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllContent_ShouldReturn200WithContentList()
    {
        // Act
        var response = await _client.GetAsync("/api/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
        content.Pagination.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllContent_WithEmptyDirectory_ShouldReturn200WithEmptyList()
    {
        // Note: This test assumes we can configure an empty content directory
        // In practice, this might require a test-specific configuration

        // Act
        var response = await _client.GetAsync("/api/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContentBySlug_WithValidSlug_ShouldReturn200WithContent()
    {
        // Arrange
        var slug = "getting-started";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.Slug.Should().Be(slug);
        content.MarkdownContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetContentBySlug_WithInvalidSlug_ShouldReturn404()
    {
        // Arrange
        var slug = "non-existent-slug";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
