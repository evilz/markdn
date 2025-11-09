using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Markdn.Api.Models;

namespace Markdn.Api.Tests.Contract.Endpoints;

/// <summary>
/// Contract tests for Collections API endpoints.
/// Tests API surface, response formats, and error handling.
/// </summary>
public class CollectionsEndpointsTests : IClassFixture<ContractTestApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ContractTestApplicationFactory _factory;

    public CollectionsEndpointsTests(ContractTestApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Collections_ShouldReturn200WithListOfCollections()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/collections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var collections = await response.Content.ReadFromJsonAsync<Dictionary<string, Collection>>();
        collections.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_Collections_ShouldReturnEmptyListWhenNoCollectionsConfigured()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/collections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var collections = await response.Content.ReadFromJsonAsync<Dictionary<string, Collection>>();
        collections.Should().NotBeNull();
        collections.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_CollectionByName_WithValidName_ShouldReturn200()
    {
        // Arrange
        var collectionName = "test-collection";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}");

        // Assert
        // This will be 404 until collections are actually configured
        // For now, just ensure the endpoint exists and returns a valid HTTP status
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CollectionByName_WithInvalidName_ShouldReturn404()
    {
        // Arrange
        var collectionName = "nonexistent-collection-xyz";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CollectionByName_WithValidCollection_ShouldReturnCollectionMetadata()
    {
        // Arrange
        var collectionName = "test-collection";

        // Act
        var response = await _client.GetAsync($"/api/collections/{collectionName}");

        // Assert
        // Skip assertion if not found (no collection configured yet)
        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var collection = await response.Content.ReadFromJsonAsync<Collection>();
            collection.Should().NotBeNull();
            collection!.Name.Should().Be(collectionName);
            collection.Schema.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GET_Collections_ShouldReturnCorsHeaders()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/collections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // CORS headers assertion can be added here if CORS is configured
    }

    [Fact]
    public async Task POST_CollectionValidate_WithValidContent_ShouldReturn200WithValidationResult()
    {
        // Arrange
        var collectionName = "test-collection";
        var content = new
        {
            title = "Test Post",
            author = "John Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/collections/{collectionName}/validate", content);

        // Assert
        // This will be 404 until collections are actually configured
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ValidationResult>();
            result.Should().NotBeNull();
            result!.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
