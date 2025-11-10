using Markdn.Api.Models;

namespace Markdn.Api.Querying;

/// <summary>
/// Interface for parsing OData-like query strings into structured query expressions.
/// </summary>
public interface IQueryParser
{
    /// <summary>
    /// Parses a query string into a QueryExpression.
    /// </summary>
    /// <param name="filter">The $filter query parameter value.</param>
    /// <param name="orderBy">The $orderby query parameter value.</param>
    /// <param name="top">The $top query parameter value.</param>
    /// <param name="skip">The $skip query parameter value.</param>
    /// <param name="schema">The collection schema for validation.</param>
    /// <returns>A parsed QueryExpression.</returns>
    /// <exception cref="ArgumentException">Thrown when query syntax is invalid or references non-existent fields.</exception>
    QueryExpression Parse(
        string? filter,
        string? orderBy,
        int? top,
        int? skip,
        CollectionSchema schema);
}
