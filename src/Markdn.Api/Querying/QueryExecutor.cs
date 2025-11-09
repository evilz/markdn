using Markdn.Api.Models;
using Microsoft.Extensions.Logging;

namespace Markdn.Api.Querying;

/// <summary>
/// Executes query expressions against collections of content items.
/// Applies filtering, sorting, pagination, and field selection.
/// </summary>
public class QueryExecutor
{
    private readonly ILogger<QueryExecutor> _logger;

    public QueryExecutor(ILogger<QueryExecutor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a query expression against a collection of content items.
    /// </summary>
    /// <param name="items">The content items to query.</param>
    /// <param name="query">The query expression to execute.</param>
    /// <returns>The filtered, sorted, and paginated results.</returns>
    public IReadOnlyList<ContentItem> Execute(IReadOnlyList<ContentItem> items, QueryExpression query)
    {
        _logger.LogDebug("Executing query on {Count} items", items.Count);

        IEnumerable<ContentItem> results = items;

        // Apply filtering
        if (query.Filter != null)
        {
            results = ApplyFilter(results, query.Filter);
            _logger.LogDebug("After filter: {Count} items", results.Count());
        }

        // Apply sorting
        if (query.OrderBy.Any())
        {
            results = ApplySort(results, query.OrderBy);
        }

        // Apply pagination
        if (query.Skip.HasValue)
        {
            results = results.Skip(query.Skip.Value);
        }

        if (query.Top.HasValue)
        {
            results = results.Take(query.Top.Value);
        }

        var finalResults = results.ToList();

        // Apply field selection (if needed)
        if (query.Select != null && query.Select.Any())
        {
            finalResults = ApplyFieldSelection(finalResults, query.Select);
        }

        _logger.LogInformation("Query execution complete: {ResultCount} items returned", finalResults.Count);

        return finalResults;
    }

    private IEnumerable<ContentItem> ApplyFilter(IEnumerable<ContentItem> items, FilterExpression filter)
    {
        foreach (var item in items)
        {
            // Convert ContentItem to dictionary for filter evaluation
            var itemDict = ConvertToFilterableData(item);
            
            if (filter.Evaluate(itemDict))
            {
                yield return item;
            }
        }
    }

    private Dictionary<string, object?> ConvertToFilterableData(ContentItem item)
    {
        // Combine standard properties and custom fields for filtering
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Add standard fields
        if (!string.IsNullOrEmpty(item.Title))
            data["title"] = item.Title;
        if (item.Date.HasValue)
            data["date"] = item.Date.Value;
        if (!string.IsNullOrEmpty(item.Author))
            data["author"] = item.Author;
        if (!string.IsNullOrEmpty(item.Category))
            data["category"] = item.Category;
        if (!string.IsNullOrEmpty(item.Description))
            data["description"] = item.Description;
        if (!string.IsNullOrEmpty(item.Slug))
            data["slug"] = item.Slug;

        // Add custom fields
        if (item.CustomFields != null)
        {
            foreach (var (key, value) in item.CustomFields)
            {
                data[key] = value;
            }
        }

        return data;
    }

    private IEnumerable<ContentItem> ApplySort(IEnumerable<ContentItem> items, List<OrderByClause> orderBy)
    {
        IOrderedEnumerable<ContentItem>? ordered = null;

        for (int i = 0; i < orderBy.Count; i++)
        {
            var clause = orderBy[i];
            var isFirst = i == 0;

            if (clause.Direction == SortDirection.Ascending)
            {
                ordered = isFirst
                    ? items.OrderBy(item => GetFieldValue(item, clause.FieldName))
                    : ordered!.ThenBy(item => GetFieldValue(item, clause.FieldName));
            }
            else
            {
                ordered = isFirst
                    ? items.OrderByDescending(item => GetFieldValue(item, clause.FieldName))
                    : ordered!.ThenByDescending(item => GetFieldValue(item, clause.FieldName));
            }
        }

        return ordered ?? items;
    }

    private object? GetFieldValue(ContentItem item, string fieldName)
    {
        // Try standard properties first
        return fieldName.ToLowerInvariant() switch
        {
            "title" => item.Title,
            "date" => item.Date,
            "author" => item.Author,
            "category" => item.Category,
            "description" => item.Description,
            "slug" => item.Slug,
            "lastmodified" => item.LastModified,
            _ => item.CustomFields?.GetValueOrDefault(fieldName)
        };
    }

    private List<ContentItem> ApplyFieldSelection(List<ContentItem> items, List<string> selectedFields)
    {
        // For now, return full items
        // Field selection would require creating a dynamic projection
        // which is complex in strongly-typed C#
        // This could be enhanced later with a custom DTO or dynamic object
        
        _logger.LogDebug("Field selection requested but returning full items: {Fields}", 
            string.Join(", ", selectedFields));
        
        return items;
    }
}
