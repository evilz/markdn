using Markdn.Api.Configuration;
using Markdn.Api.FileSystem;
using Markdn.Api.Middleware;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<MarkdnOptions>(
    builder.Configuration.GetSection("Markdn"));

// Register services
builder.Services.AddSingleton<FrontMatterParser>();
builder.Services.AddSingleton<MarkdownParser>();
builder.Services.AddSingleton<SlugGenerator>();
builder.Services.AddSingleton<IContentRepository, FileSystemContentRepository>();
builder.Services.AddScoped<ContentService>();

// Add health checks
builder.Services.AddHealthChecks();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
    int page = 1,
    int pageSize = 50,
    CancellationToken cancellationToken = default) =>
{
    if (page < 1 || pageSize < 1 || pageSize > 100)
    {
        return TypedResults.BadRequest();
    }

    var collection = await contentService.GetAllAsync(page, pageSize, cancellationToken);
    
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

app.MapGet("/api/content/{slug}", async Task<Results<Ok<ContentItemResponse>, NotFound>> (
    string slug,
    ContentService contentService,
    CancellationToken cancellationToken) =>
{
    var item = await contentService.GetBySlugAsync(slug, cancellationToken);
    
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

// Map health check endpoint
app.MapHealthChecks("/api/health");

app.Run();

// Make Program accessible for testing
public partial class Program { }
