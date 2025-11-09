using System.Text.Json;
using Markdn.Api.Models;
using Markdn.Api.Querying;
using Markdn.Api.Services;
using Markdn.Api.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Markdn.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Collections feature.
/// </summary>
public static class CollectionsEndpoints
{
    /// <summary>
    /// Maps all collection endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapCollectionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collections")
            .WithTags("Collections")
            .WithOpenApi();

        group.MapGet("/", GetAllCollectionsAsync)
            .WithName("GetAllCollections")
            .WithSummary("Get all available collections")
            .Produces<Dictionary<string, Collection>>(StatusCodes.Status200OK);

        group.MapGet("/{name}", GetCollectionByNameAsync)
            .WithName("GetCollectionByName")
            .WithSummary("Get a specific collection by name")
            .Produces<Collection>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{name}/validate", ValidateContentAsync)
            .WithName("ValidateContent")
            .WithSummary("Validate content against a collection schema")
            .Produces<ValidationResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{name}/items", GetCollectionItemsAsync)
            .WithName("GetCollectionItems")
            .WithSummary("Get all items from a collection")
            .Produces<CollectionItemsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{name}/items/{id}", GetCollectionItemByIdAsync)
            .WithName("GetCollectionItemById")
            .WithSummary("Get a specific item from a collection by slug/ID")
            .Produces<ContentItem>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<Dictionary<string, Collection>>, ProblemHttpResult>> GetAllCollectionsAsync(
        ICollectionLoader loader,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Retrieving all collections");
            var collections = await loader.LoadCollectionsAsync(cancellationToken);
            logger.LogInformation("Retrieved {Count} collections", collections.Count);
            return TypedResults.Ok(collections);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Collections configuration file not found");
            // Return empty dictionary instead of error if config file doesn't exist
            return TypedResults.Ok(new Dictionary<string, Collection>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving collections");
            return TypedResults.Problem(
                title: "Error retrieving collections",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<Collection>, NotFound, ProblemHttpResult>> GetCollectionByNameAsync(
        string name,
        ICollectionLoader loader,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Retrieving collection {CollectionName}", name);
            var collection = await loader.LoadCollectionAsync(name, cancellationToken);

            if (collection == null)
            {
                logger.LogWarning("Collection {CollectionName} not found", name);
                return TypedResults.NotFound();
            }

            logger.LogInformation("Retrieved collection {CollectionName}", name);
            return TypedResults.Ok(collection);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Collections configuration file not found");
            // Return 404 if config file doesn't exist (no collections available)
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving collection {CollectionName}", name);
            return TypedResults.Problem(
                title: $"Error retrieving collection {name}",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<ValidationResult>, NotFound, ProblemHttpResult>> ValidateContentAsync(
        string name,
        JsonElement content,
        ICollectionLoader loader,
        ContentItemValidator validator,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Validating content against collection {CollectionName}", name);
            
            // Load the collection schema
            var collection = await loader.LoadCollectionAsync(name, cancellationToken);
            if (collection == null)
            {
                logger.LogWarning("Collection {CollectionName} not found", name);
                return TypedResults.NotFound();
            }

            // Convert JsonElement to Dictionary for ContentItem
            var contentDict = JsonSerializer.Deserialize<Dictionary<string, object>>(content.GetRawText());
            
            // Create a temporary ContentItem for validation
            var contentItem = new ContentItem
            {
                Slug = contentDict?.GetValueOrDefault("slug")?.ToString() ?? "temp",
                FilePath = "temp",
                CustomFields = contentDict ?? new Dictionary<string, object>()
            };

            // Validate the content
            var result = await validator.ValidateAsync(contentItem, collection.Schema, cancellationToken);

            logger.LogInformation("Content validation completed: IsValid={IsValid}, Errors={ErrorCount}", 
                result.IsValid, result.Errors.Count);
            
            return TypedResults.Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Collections configuration file not found");
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating content for collection {CollectionName}", name);
            return TypedResults.Problem(
                title: $"Error validating content for collection {name}",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<CollectionItemsResponse>, NotFound, ProblemHttpResult>> GetCollectionItemsAsync(
        string name,
        ICollectionService collectionService,
        ICollectionLoader collectionLoader,
        IQueryParser queryParser,
        ILogger<Program> logger,
        string? filter = null,
        string? orderby = null,
        int? top = null,
        int? skip = null,
        string? select = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting items from collection {CollectionName}", name);
            
            // Check if query parameters are provided
            var hasQuery = !string.IsNullOrWhiteSpace(filter) ||
                          !string.IsNullOrWhiteSpace(orderby) ||
                          top.HasValue ||
                          skip.HasValue ||
                          !string.IsNullOrWhiteSpace(select);

            IReadOnlyList<ContentItem> items;

            if (hasQuery)
            {
                // Load collection schema for query validation
                var collection = await collectionLoader.LoadCollectionAsync(name, cancellationToken);
                if (collection == null)
                {
                    logger.LogWarning("Collection {CollectionName} not found", name);
                    return TypedResults.NotFound();
                }

                // Parse and validate query
                var query = queryParser.Parse(filter, orderby, top, skip, select, collection.Schema);
                
                // Execute query
                items = await collectionService.QueryAsync(name, query, cancellationToken);
            }
            else
            {
                // No query parameters - get all items
                items = await collectionService.GetAllItemsAsync(name, cancellationToken);

                if (!items.Any())
                {
                    // Check if collection exists
                    var collections = await collectionService.GetAllCollectionsAsync(cancellationToken);
                    if (!collections.ContainsKey(name))
                    {
                        logger.LogWarning("Collection {CollectionName} not found", name);
                        return TypedResults.NotFound();
                    }
                }
            }

            var response = new CollectionItemsResponse
            {
                Items = items.ToList(),
                TotalCount = items.Count
            };

            logger.LogInformation("Retrieved {Count} items from collection {CollectionName}", items.Count, name);
            return TypedResults.Ok(response);
        }
        catch (ArgumentException ex)
        {
            // Query validation error
            logger.LogWarning(ex, "Invalid query for collection {CollectionName}", name);
            return TypedResults.Problem(
                title: "Invalid query syntax",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving items from collection {CollectionName}", name);
            return TypedResults.Problem(
                title: $"Error retrieving items from collection {name}",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<Ok<ContentItem>, NotFound, ProblemHttpResult>> GetCollectionItemByIdAsync(
        string name,
        string id,
        ICollectionService collectionService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting item {ItemId} from collection {CollectionName}", id, name);
            
            var item = await collectionService.GetItemByIdAsync(name, id, cancellationToken);

            if (item == null)
            {
                logger.LogWarning("Item {ItemId} not found in collection {CollectionName}", id, name);
                return TypedResults.NotFound();
            }

            logger.LogInformation("Retrieved item {ItemId} from collection {CollectionName}", id, name);
            return TypedResults.Ok(item);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving item {ItemId} from collection {CollectionName}", id, name);
            return TypedResults.Problem(
                title: $"Error retrieving item {id} from collection {name}",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Response model for collection items query.
    /// </summary>
    public class CollectionItemsResponse
    {
        public List<ContentItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
