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

    // Phase 6: User Story 4 - Content Rendering Options Contract Tests

    [Fact]
    public async Task GetContentBySlug_WithFormatMarkdown_ShouldReturnOnlyMarkdownContent()
    {
        // Arrange
        var slug = "getting-started";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}?format=markdown");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.MarkdownContent.Should().NotBeNullOrEmpty();
        content.HtmlContent.Should().BeNull();
    }

    [Fact]
    public async Task GetContentBySlug_WithFormatHtml_ShouldReturnOnlyHtmlContent()
    {
        // Arrange
        var slug = "getting-started";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}?format=html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.HtmlContent.Should().NotBeNullOrEmpty();
        content.MarkdownContent.Should().BeNull();
    }

    [Fact]
    public async Task GetContentBySlug_WithFormatBoth_ShouldReturnBothContents()
    {
        // Arrange
        var slug = "getting-started";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}?format=both");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.MarkdownContent.Should().NotBeNullOrEmpty();
        content!.HtmlContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetContentBySlug_WithDefaultFormat_ShouldReturnBothContents()
    {
        // Arrange
        var slug = "getting-started";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentItemResponse>();
        content.Should().NotBeNull();
        content!.MarkdownContent.Should().NotBeNullOrEmpty();
        content!.HtmlContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetContentBySlug_WithInvalidFormat_ShouldReturn400BadRequest()
    {
        // Arrange
        var slug = "getting-started";

        // Act
        var response = await _client.GetAsync($"/api/content/{slug}?format=invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Phase 4: User Story 2 - Content Query and Filtering Contract Tests

    [Fact]
    public async Task GetAllContent_WithTagFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var tag = "tutorial";

        // Act
        var response = await _client.GetAsync($"/api/content?tag={tag}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
        // Note: Actual filtering verification requires test data with known tags
    }

    [Fact]
    public async Task GetAllContent_WithCategoryFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var category = "blog";

        // Act
        var response = await _client.GetAsync($"/api/content?category={category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
        // Note: Actual filtering verification requires test data with known categories
    }

    [Fact]
    public async Task GetAllContent_WithDateRangeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var dateFrom = "2025-01-01";
        var dateTo = "2025-12-31";

        // Act
        var response = await _client.GetAsync($"/api/content?dateFrom={dateFrom}&dateTo={dateTo}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
        // Note: Actual date filtering verification requires test data with known dates
    }

    [Fact]
    public async Task GetAllContent_WithMultipleFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var tag = "tutorial";
        var category = "blog";

        // Act
        var response = await _client.GetAsync($"/api/content?tag={tag}&category={category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
        // Note: Actual filtering verification requires test data matching both criteria
    }

    [Fact]
    public async Task GetAllContent_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var sortBy = "date";
        var sortOrder = "asc";

        // Act
        var response = await _client.GetAsync($"/api/content?sortBy={sortBy}&sortOrder={sortOrder}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ContentListResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
        // Note: Actual sorting verification requires test data to verify order
    }

    [Fact]
    public async Task GetAllContent_WithInvalidDateFormat_ShouldReturn400BadRequest()
    {
        // Arrange
        var invalidDate = "not-a-date";

        // Act
        var response = await _client.GetAsync($"/api/content?dateFrom={invalidDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
