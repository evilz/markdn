using System.Diagnostics;
using Markdn.Api.Configuration;
using Markdn.Api.Endpoints;
using Markdn.Api.FileSystem;
using Markdn.Api.HealthChecks;
using Markdn.Api.HostedServices;
using Markdn.Api.Middleware;
using Markdn.Api.Models;
using Markdn.Api.Querying;
using Markdn.Api.Services;
using Markdn.Api.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

// Create ActivitySource for distributed tracing
var activitySource = new ActivitySource("Markdn.Api", "1.0.0");

var builder = WebApplication.CreateBuilder(args);

// Remove server header for security
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Validate service provider configuration in development
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

// Configure options
builder.Services.Configure<MarkdnOptions>(
    builder.Configuration.GetSection("Markdn"));

builder.Services.Configure<CollectionsOptions>(
    builder.Configuration.GetSection(CollectionsOptions.SectionName));

// Add memory cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100; // Limit to 100 items
});

// Register ActivitySource for distributed tracing
builder.Services.AddSingleton(activitySource);

// Register services
builder.Services.AddSingleton<FrontMatterParser>();
builder.Services.AddSingleton<MarkdownParser>();
builder.Services.AddSingleton<SlugGenerator>();
builder.Services.AddSingleton<IContentRepository, FileSystemContentRepository>();
builder.Services.AddSingleton<IContentCache, ContentCache>();
builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();
builder.Services.AddHostedService<FileWatcherHostedService>();
builder.Services.AddScoped<ContentService>();

// Register Collection services
builder.Services.AddSingleton<ICollectionLoader, CollectionLoader>();
builder.Services.AddSingleton<ISchemaValidator, SchemaValidator>();
builder.Services.AddScoped<ContentItemValidator>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddHostedService<CollectionFileWatcherService>();

// Register Query services
builder.Services.AddScoped<IQueryParser, QueryParser>();
builder.Services.AddScoped<QueryExecutor>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<CollectionHealthCheck>("collections", tags: new[] { "ready", "collections" });

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
});

// Add global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map content endpoints
app.MapGet("/api/content", async Task<Results<Ok<ContentListResponse>, BadRequest>> (
    ContentService contentService,
    string? tag = null,
    string? category = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    int page = 1,
    int pageSize = 50,
    string sortBy = "date",
    string sortOrder = "desc",
    CancellationToken cancellationToken = default) =>
{
    if (page < 1 || pageSize < 1 || pageSize > 100)
    {
        return TypedResults.BadRequest();
    }

    // Security: Validate tag input (prevent injection)
    if (!string.IsNullOrEmpty(tag) && (tag.Length > 100 || tag.Contains('\0')))
    {
        return TypedResults.BadRequest();
    }

    // Security: Validate category input (prevent injection)
    if (!string.IsNullOrEmpty(category) && (category.Length > 100 || category.Contains('\0')))
    {
        return TypedResults.BadRequest();
    }

    // Validate sortBy values
    var validSortBy = new[] { "date", "title", "lastmodified" };
    if (!validSortBy.Contains(sortBy.ToLowerInvariant()))
    {
        return TypedResults.BadRequest();
    }

    // Validate sortOrder values
    var validSortOrder = new[] { "asc", "desc" };
    if (!validSortOrder.Contains(sortOrder.ToLowerInvariant()))
    {
        return TypedResults.BadRequest();
    }

    var query = new ContentQueryRequest
    {
        Tag = tag,
        Category = category,
        DateFrom = dateFrom,
        DateTo = dateTo,
        Page = page,
        PageSize = pageSize,
        SortBy = sortBy,
        SortOrder = sortOrder
    };

    var collection = await contentService.GetAllAsync(query, page, pageSize, cancellationToken);

    var response = new ContentListResponse
    {
        Items = collection.Items.Select(item => new ContentItemSummary
        {
            Slug = item.Slug,
            Title = item.Title,
            Date = item.Date,
            Author = item.Author,
            Category = item.Category,
            Description = item.Description,
            Tags = item.Tags
        }).ToList(),
        Pagination = new PaginationMetadata
        {
            TotalCount = collection.TotalCount,
            Page = collection.Page,
            PageSize = collection.PageSize,
            TotalPages = collection.TotalPages,
            HasPrevious = collection.HasPrevious,
            HasNext = collection.HasNext
        }
    };

    return TypedResults.Ok(response);
})
.WithName("GetAllContent")
.WithOpenApi();

app.MapGet("/api/content/{slug}", async Task<Results<Ok<ContentItemResponse>, NotFound, BadRequest<string>>> (
    string slug,
    ContentService contentService,
    string? format,
    CancellationToken cancellationToken) =>
{
    // Parse and validate format parameter
    FormatOption formatOption = FormatOption.Both; // Default
    if (!string.IsNullOrEmpty(format))
    {
        if (!Enum.TryParse<FormatOption>(format, ignoreCase: true, out formatOption))
        {
            return TypedResults.BadRequest($"Invalid format value: '{format}'. Valid values are: markdown, html, both");
        }
    }

    var item = await contentService.GetBySlugAsync(slug, formatOption, cancellationToken);

    if (item == null)
    {
        return TypedResults.NotFound();
    }

    var response = new ContentItemResponse
    {
        Slug = item.Slug,
        Title = item.Title,
        Date = item.Date,
        Author = item.Author,
        Tags = item.Tags,
        Category = item.Category,
        Description = item.Description,
        CustomFields = item.CustomFields,
        MarkdownContent = item.MarkdownContent,
        HtmlContent = item.HtmlContent,
        LastModified = item.LastModified,
        Warnings = item.HasParsingErrors ? item.ParsingWarnings : null
    };

    return TypedResults.Ok(response);
})
.WithName("GetContentBySlug")
.WithOpenApi();

// Map Collections endpoints
app.MapCollectionsEndpoints();

// Map health check endpoint
app.MapHealthChecks("/api/health");

app.Run();

// Make Program accessible for testing
public partial class Program { }
