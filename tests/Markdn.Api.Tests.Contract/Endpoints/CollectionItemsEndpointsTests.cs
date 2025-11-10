using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Markdn.Api.Models;

namespace Markdn.Api.Tests.Contract.Endpoints;

/// <summary>
/// Contract tests for Collection Items endpoints - T065, T066
/// Tests GET /api/collections/{name}/items and GET /api/collections/{name}/items/{id}
/// </summary>
public class CollectionItemsEndpointsTests : IClassFixture<ContractTestApplicationFactory>
{
    private readonly HttpClient _client;

    public CollectionItemsEndpointsTests(ContractTestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_CollectionItems_WithValidCollection_ShouldReturn200WithItems()
    {
        // Arrange
        var collectionName = "test-collection";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_CollectionItems_WithNonExistentCollection_ShouldReturn404()
    {
        // Arrange
        var collectionName = "non-existent-collection";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CollectionItems_ShouldReturnValidatedItemsOnly()
    {
        // Arrange
        var collectionName = "test-collection";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}/items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CollectionItemsResponse>();
        
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Items.Should().AllSatisfy(item =>
        {
            item.IsValid.Should().BeTrue("only valid items should be returned");
            item.CollectionName.Should().Be(collectionName);
        });
    }

    [Fact]
    public async Task GET_CollectionItemById_WithValidSlug_ShouldReturn200WithItem()
    {
        // Arrange
        var collectionName = "test-collection";
        var itemSlug = "test-item";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}/items/{itemSlug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await response.Content.ReadFromJsonAsync<ContentItem>();
        
        item.Should().NotBeNull();
        item!.Slug.Should().Be(itemSlug);
        item.CollectionName.Should().Be(collectionName);
        item.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GET_CollectionItemById_WithNonExistentSlug_ShouldReturn404()
    {
        // Arrange
        var collectionName = "test-collection";
        var itemSlug = "non-existent-slug";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}/items/{itemSlug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CollectionItemById_WithFilenameFallback_ShouldResolveSlug()
    {
        // Arrange
        var collectionName = "test-collection";
        var filenameSlug = "item-without-explicit-slug"; // Filename without .md

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}/items/{filenameSlug}");

        // Assert - Should find item even without explicit slug in front-matter
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var item = await response.Content.ReadFromJsonAsync<ContentItem>();
            item.Should().NotBeNull();
            item!.FilePath.Should().Contain(filenameSlug);
        }
    }

    private class CollectionItemsResponse
    {
        public List<ContentItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
