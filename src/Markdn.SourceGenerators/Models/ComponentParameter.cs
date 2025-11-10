namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Represents a parameter passed to a component reference.
/// </summary>
public sealed class ComponentParameter
{
    /// <summary>
    /// Parameter name (e.g., "Severity", "OnClick")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Parameter value (string literal or expression)
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Whether this is an expression (starts with @) or a literal string
    /// </summary>
    public required bool IsExpression { get; init; }
}
