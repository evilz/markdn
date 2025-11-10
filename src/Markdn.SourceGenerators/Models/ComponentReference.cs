using System.Collections.Generic;

namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Represents a Blazor component reference within Markdown.
/// </summary>
public sealed class ComponentReference
{
    /// <summary>
    /// Component type name (e.g., "Counter", "Alert")
    /// </summary>
    public required string ComponentName { get; init; }

    /// <summary>
    /// Component parameters
    /// </summary>
    public required IReadOnlyList<ComponentParameter> Parameters { get; init; }

    /// <summary>
    /// Child content (for components with body)
    /// </summary>
    public string? ChildContent { get; init; }

    /// <summary>
    /// Whether this is a self-closing tag (&lt;Component /&gt;)
    /// </summary>
    public required bool IsSelfClosing { get; init; }
}
