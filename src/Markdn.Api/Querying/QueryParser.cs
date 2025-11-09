using Markdn.Api.Models;
using Microsoft.Extensions.Logging;

namespace Markdn.Api.Querying;

/// <summary>
/// Parses OData-like query strings into structured QueryExpression objects.
/// Supports $filter, $orderby, $top, $skip, and $select.
/// </summary>
public class QueryParser : IQueryParser
{
    private readonly ILogger<QueryParser> _logger;

    public QueryParser(ILogger<QueryParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public QueryExpression Parse(
        string? filter,
        string? orderBy,
        int? top,
        int? skip,
        string? select,
        CollectionSchema schema)
    {
        _logger.LogDebug("Parsing query: filter={Filter}, orderBy={OrderBy}, top={Top}, skip={Skip}, select={Select}",
            filter, orderBy, top, skip, select);

        var query = new QueryExpression
        {
            Top = top,
            Skip = skip
        };

        // Parse $filter
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query.Filter = ParseFilter(filter, schema);
        }

        // Parse $orderby
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            query.OrderBy = ParseOrderBy(orderBy, schema);
        }

        // Parse $select
        if (!string.IsNullOrWhiteSpace(select))
        {
            query.Select = ParseSelect(select, schema);
        }

        ValidateQuery(query, schema);

        return query;
    }

    private FilterExpression ParseFilter(string filter, CollectionSchema schema)
    {
        // Simple recursive descent parser for filter expressions
        // Supports: field op value, expr and expr, expr or expr
        
        filter = filter.Trim();

        // Check for logical operators (and/or) - simple split approach
        var andIndex = FindLogicalOperator(filter, " and ");
        if (andIndex > 0)
        {
            var left = filter[..andIndex].Trim();
            var right = filter[(andIndex + 5)..].Trim();
            
            return new LogicalExpression
            {
                Left = ParseFilter(left, schema),
                Operator = LogicalOperator.And,
                Right = ParseFilter(right, schema)
            };
        }

        var orIndex = FindLogicalOperator(filter, " or ");
        if (orIndex > 0)
        {
            var left = filter[..orIndex].Trim();
            var right = filter[(orIndex + 4)..].Trim();
            
            return new LogicalExpression
            {
                Left = ParseFilter(left, schema),
                Operator = LogicalOperator.Or,
                Right = ParseFilter(right, schema)
            };
        }

        // Parse comparison expression: field op value
        return ParseComparison(filter, schema);
    }

    private int FindLogicalOperator(string filter, string op)
    {
        // Find operator at top level (not inside parentheses or quotes)
        int depth = 0;
        bool inQuote = false;
        
        for (int i = 0; i < filter.Length - op.Length + 1; i++)
        {
            if (filter[i] == '\'' && (i == 0 || filter[i - 1] != '\\'))
            {
                inQuote = !inQuote;
            }
            else if (!inQuote)
            {
                if (filter[i] == '(') depth++;
                else if (filter[i] == ')') depth--;
                else if (depth == 0 && filter.Substring(i, op.Length).Equals(op, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        
        return -1;
    }

    private ComparisonExpression ParseComparison(string expression, CollectionSchema schema)
    {
        // Parse: fieldName operator value
        // Operators: eq, ne, gt, lt, ge, le, contains, startswith, endswith
        
        var operators = new[]
        {
            (" eq ", ComparisonOperator.Equal),
            (" ne ", ComparisonOperator.NotEqual),
            (" gt ", ComparisonOperator.GreaterThan),
            (" ge ", ComparisonOperator.GreaterThanOrEqual),
            (" lt ", ComparisonOperator.LessThan),
            (" le ", ComparisonOperator.LessThanOrEqual),
        };

        // Also check for function operators
        if (expression.Contains("contains(", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFunctionOperator(expression, "contains", ComparisonOperator.Contains, schema);
        }
        if (expression.Contains("startswith(", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFunctionOperator(expression, "startswith", ComparisonOperator.StartsWith, schema);
        }
        if (expression.Contains("endswith(", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFunctionOperator(expression, "endswith", ComparisonOperator.EndsWith, schema);
        }

        foreach (var (opStr, opEnum) in operators)
        {
            var opIndex = expression.IndexOf(opStr, StringComparison.OrdinalIgnoreCase);
            if (opIndex > 0)
            {
                var fieldName = expression[..opIndex].Trim();
                var valueStr = expression[(opIndex + opStr.Length)..].Trim();

                ValidateFieldName(fieldName, schema);
                var value = ParseValue(valueStr);

                return new ComparisonExpression
                {
                    FieldName = fieldName,
                    Operator = opEnum,
                    Value = value
                };
            }
        }

        throw new ArgumentException($"Invalid filter expression: {expression}");
    }

    private ComparisonExpression ParseFunctionOperator(string expression, string function, ComparisonOperator op, CollectionSchema schema)
    {
        // Parse: function(fieldName, 'value')
        var startIndex = expression.IndexOf('(');
        var endIndex = expression.LastIndexOf(')');
        
        if (startIndex < 0 || endIndex < 0)
        {
            throw new ArgumentException($"Invalid {function} expression: {expression}");
        }

        var args = expression.Substring(startIndex + 1, endIndex - startIndex - 1);
        var parts = args.Split(',', 2);
        
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid {function} expression: expected 2 arguments");
        }

        var fieldName = parts[0].Trim();
        var valueStr = parts[1].Trim();

        ValidateFieldName(fieldName, schema);
        var value = ParseValue(valueStr);

        return new ComparisonExpression
        {
            FieldName = fieldName,
            Operator = op,
            Value = value
        };
    }

    private object ParseValue(string valueStr)
    {
        valueStr = valueStr.Trim();

        // String literal: 'value'
        if (valueStr.StartsWith('\'') && valueStr.EndsWith('\''))
        {
            return valueStr[1..^1];
        }

        // Boolean
        if (valueStr.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (valueStr.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        // Number
        if (int.TryParse(valueStr, out var intValue))
            return intValue;
        if (double.TryParse(valueStr, out var doubleValue))
            return doubleValue;

        // DateTime (ISO format)
        if (DateTime.TryParse(valueStr, out var dateValue))
            return dateValue;

        // Default to string
        return valueStr;
    }

    private List<OrderByClause> ParseOrderBy(string orderBy, CollectionSchema schema)
    {
        var clauses = new List<OrderByClause>();
        var parts = orderBy.Split(',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0 || tokens.Length > 2)
            {
                throw new ArgumentException($"Invalid orderby clause: {part}");
            }

            var fieldName = tokens[0];
            var direction = tokens.Length == 2 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Descending
                : SortDirection.Ascending;

            ValidateFieldName(fieldName, schema);

            clauses.Add(new OrderByClause
            {
                FieldName = fieldName,
                Direction = direction
            });
        }

        return clauses;
    }

    private List<string> ParseSelect(string select, CollectionSchema schema)
    {
        var fields = select.Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        foreach (var field in fields)
        {
            ValidateFieldName(field, schema);
        }

        return fields;
    }

    private void ValidateFieldName(string fieldName, CollectionSchema schema)
    {
        if (!schema.Properties.ContainsKey(fieldName))
        {
            throw new ArgumentException($"Field '{fieldName}' does not exist in schema. Available fields: {string.Join(", ", schema.Properties.Keys)}");
        }
    }

    private void ValidateQuery(QueryExpression query, CollectionSchema schema)
    {
        // Validate $top
        if (query.Top.HasValue && query.Top.Value <= 0)
        {
            throw new ArgumentException("$top must be greater than 0");
        }

        // Validate $skip
        if (query.Skip.HasValue && query.Skip.Value < 0)
        {
            throw new ArgumentException("$skip must be greater than or equal to 0");
        }

        _logger.LogDebug("Query validation passed");
    }
}
