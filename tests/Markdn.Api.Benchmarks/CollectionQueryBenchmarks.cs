using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Markdn.Api.Configuration;
using Markdn.Api.Models;
using Markdn.Api.Querying;
using Markdn.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Markdn.Api.Benchmarks;

/// <summary>
/// Benchmarks for collection query operations to measure performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CollectionQueryBenchmarks
{
    private CollectionService? _collectionService;
    private QueryParser? _queryParser;
    private List<ContentItem>? _testItems;
    private const string TestCollectionName = "blog";

    [GlobalSetup]
    public void Setup()
    {
        // Create test data
        _testItems = new List<ContentItem>();
        for (int i = 0; i < 1000; i++)
        {
            var item = new ContentItem
            {
                Identifier = new ContentIdentifier
                {
                    Slug = $"post-{i}",
                    FilePath = $"content/blog/post-{i}.md"
                },
                FrontMatter = new Dictionary<string, object>
                {
                    ["title"] = $"Post {i}",
                    ["author"] = i % 10 == 0 ? "Alice" : "Bob",
                    ["date"] = DateTime.UtcNow.AddDays(-i),
                    ["category"] = i % 3 == 0 ? "Tech" : i % 3 == 1 ? "Business" : "Lifestyle",
                    ["views"] = i * 100
                },
                MarkdownContent = $"# Post {i}\n\nContent for post {i}",
                HtmlContent = $"<h1>Post {i}</h1><p>Content for post {i}</p>",
                ValidationResult = new ValidationResult { IsValid = true }
            };
            _testItems.Add(item);
        }

        // Setup services
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new MarkdnOptions { ContentPath = "content" });
        var frontMatterParser = new FrontMatterParser(NullLogger<FrontMatterParser>.Instance);
        var markdownParser = new MarkdownParser();
        var slugGenerator = new SlugGenerator();
        var queryExecutor = new QueryExecutor(NullLogger<QueryExecutor>.Instance);
        var activitySource = new ActivitySource("Markdn.Api.Benchmarks");
        var meterFactory = new TestMeterFactory();

        var collectionLoader = new TestCollectionLoader();
        _collectionService = new CollectionService(
            collectionLoader,
            frontMatterParser,
            markdownParser,
            slugGenerator,
            cache,
            options,
            queryExecutor,
            activitySource,
            meterFactory,
            NullLogger<CollectionService>.Instance);

        var schemaValidator = new SchemaValidator(NullLogger<SchemaValidator>.Instance);
        _queryParser = new QueryParser(schemaValidator, NullLogger<QueryParser>.Instance);
    }

    [Benchmark(Description = "Query with simple filter (author eq 'Alice')")]
    public async Task<IReadOnlyList<ContentItem>> QuerySimpleFilter()
    {
        var query = _queryParser!.Parse("$filter=author eq 'Alice'", TestCollectionName);
        return await _collectionService!.QueryAsync(TestCollectionName, query);
    }

    [Benchmark(Description = "Query with multiple filters (author and category)")]
    public async Task<IReadOnlyList<ContentItem>> QueryMultipleFilters()
    {
        var query = _queryParser!.Parse("$filter=author eq 'Alice' and category eq 'Tech'", TestCollectionName);
        return await _collectionService!.QueryAsync(TestCollectionName, query);
    }

    [Benchmark(Description = "Query with sorting ($orderby=date desc)")]
    public async Task<IReadOnlyList<ContentItem>> QueryWithSorting()
    {
        var query = _queryParser!.Parse("$orderby=date desc", TestCollectionName);
        return await _collectionService!.QueryAsync(TestCollectionName, query);
    }

    [Benchmark(Description = "Query with pagination ($top=10&$skip=20)")]
    public async Task<IReadOnlyList<ContentItem>> QueryWithPagination()
    {
        var query = _queryParser!.Parse("$top=10&$skip=20", TestCollectionName);
        return await _collectionService!.QueryAsync(TestCollectionName, query);
    }

    [Benchmark(Description = "Query with filter, sort, and pagination")]
    public async Task<IReadOnlyList<ContentItem>> QueryComplex()
    {
        var query = _queryParser!.Parse("$filter=category eq 'Tech'&$orderby=views desc&$top=10", TestCollectionName);
        return await _collectionService!.QueryAsync(TestCollectionName, query);
    }

    // Test implementation of ICollectionLoader
    private class TestCollectionLoader : ICollectionLoader
    {
        public Task<Collection?> LoadCollectionAsync(string name, CancellationToken cancellationToken = default)
        {
            var schema = new CollectionSchema
            {
                Fields = new Dictionary<string, FieldDefinition>
                {
                    ["title"] = new FieldDefinition { Type = FieldType.String, Required = true },
                    ["author"] = new FieldDefinition { Type = FieldType.String, Required = false },
                    ["date"] = new FieldDefinition { Type = FieldType.DateTime, Required = false },
                    ["category"] = new FieldDefinition { Type = FieldType.String, Required = false },
                    ["views"] = new FieldDefinition { Type = FieldType.Number, Required = false }
                }
            };

            var collection = new Collection
            {
                Name = name,
                Schema = schema,
                Directory = $"content/{name}"
            };

            return Task.FromResult<Collection?>(collection);
        }

        public Task<Dictionary<string, Collection>> LoadCollectionsAsync(CancellationToken cancellationToken = default)
        {
            var collections = new Dictionary<string, Collection>();
            return Task.FromResult(collections);
        }
    }

    // Test implementation of IMeterFactory
    private class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);
        public void Dispose() { }
    }
}

/// <summary>
/// Entry point for running benchmarks.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<CollectionQueryBenchmarks>();
        Console.WriteLine(summary);
    }
}
