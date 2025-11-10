using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Markdn.Api.Querying;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Markdn.Api.Benchmarks;

/// <summary>
/// Performance benchmarks for query operations.
/// Run with: dotnet run -c Release --project tests/Markdn.Api.Benchmarks
/// </summary>
[MemoryDiagnoser]
public class QueryBenchmarks
{
    private List<ContentItem> _items = null!;
    private QueryExecutor _queryExecutor = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create sample data
        _items = Enumerable.Range(1, 100).Select(i => new ContentItem
        {
            Id = $"item-{i}",
            Slug = $"item-{i}",
            Title = $"Test Item {i}",
            FilePath = $"/content/item-{i}.md",
            Content = $"Content for item {i}",
            HtmlContent = $"<p>Content for item {i}</p>",
            Data = new Dictionary<string, object>
            {
                ["published"] = i % 2 == 0,
                ["category"] = i % 3 == 0 ? "featured" : "normal",
                ["views"] = i * 10
            },
            IsValid = true,
            ValidationErrors = new List<string>(),
            ParsedAt = DateTime.UtcNow
        }).ToList();

        _queryExecutor = new QueryExecutor(NullLogger<QueryExecutor>.Instance);
    }

    [Benchmark]
    public List<ContentItem> FilterBooleanField()
    {
        var filter = new FilterExpression
        {
            Field = "published",
            Operator = "equals",
            Value = true
        };

        return _queryExecutor.Execute(_items, new ContentQuery
        {
            Filters = new List<FilterExpression> { filter }
        });
    }

    [Benchmark]
    public List<ContentItem> FilterStringField()
    {
        var filter = new FilterExpression
        {
            Field = "category",
            Operator = "equals",
            Value = "featured"
        };

        return _queryExecutor.Execute(_items, new ContentQuery
        {
            Filters = new List<FilterExpression> { filter }
        });
    }

    [Benchmark]
    public List<ContentItem> SortByNumericField()
    {
        return _queryExecutor.Execute(_items, new ContentQuery
        {
            Sort = new SortOptions
            {
                Field = "views",
                Direction = "desc"
            }
        });
    }

    [Benchmark]
    public List<ContentItem> FilterAndSort()
    {
        var filter = new FilterExpression
        {
            Field = "published",
            Operator = "equals",
            Value = true
        };

        return _queryExecutor.Execute(_items, new ContentQuery
        {
            Filters = new List<FilterExpression> { filter },
            Sort = new SortOptions
            {
                Field = "views",
                Direction = "desc"
            }
        });
    }

    [Benchmark]
    public List<ContentItem> LimitAndOffset()
    {
        return _queryExecutor.Execute(_items, new ContentQuery
        {
            Limit = 10,
            Offset = 20
        });
    }
}
