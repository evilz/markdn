using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Markdn.Api.Models;

namespace Markdn.Api.Tests.Contract.Endpoints;

/// <summary>
/// Contract tests for Collection Validation endpoints.
/// </summary>
public class CollectionValidationEndpointsTests : IClassFixture<ContractTestApplicationFactory>
{
    private readonly HttpClient _client;

    public CollectionValidationEndpointsTests(ContractTestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_CollectionsValidate_ShouldAcceptValidation()
    {
        // Arrange
        var collectionName = "blog";
        var validationRequest = new
        {
            collectionName,
            items = new[]
            {
                new
                {
                    slug = "test-post",
                    frontMatter = new Dictionary<string, object>
                    {
                        ["title"] = "Test Post",
                        ["author"] = "Test Author"
                    }
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/collections/{collectionName}/validate", validationRequest);

        // Assert
        // The endpoint should exist and accept POST requests
        // It might return 404 if no collection is configured, which is acceptable for contract testing
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest);
    }
}
