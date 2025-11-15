using System.Collections.Generic;

namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Strongly-typed representation of YAML front matter configuration.
/// </summary>
public sealed class ComponentMetadata
{
    /// <summary>
    /// Page title for PageTitle component (e.g., "About Us")
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Override default namespace (e.g., "MyApp.CustomPages")
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Using directives to add (e.g., ["MyApp.Services", "System.Text.Json"])
    /// </summary>
    public IReadOnlyList<string>? Using { get; init; }

    /// <summary>
    /// Explicit component namespaces to consider when resolving component references
    /// (e.g., ["MyApp.Components", "MyApp.Components.Shared"]). If provided, the
    /// generator will prefer these namespaces and emit using directives for them.
    /// </summary>
    public IReadOnlyList<string>? ComponentNamespaces { get; init; }

    /// <summary>
    /// Layout component name (e.g., "MainLayout")
    /// </summary>
    public string? Layout { get; init; }

    /// <summary>
    /// Base class override (default: ComponentBase)
    /// </summary>
    public string? Inherit { get; init; }

    /// <summary>
    /// Attributes to apply to class (e.g., ["Authorize(Roles = \"Admin\")"])
    /// </summary>
    public IReadOnlyList<string>? Attribute { get; init; }

    /// <summary>
    /// Content slug used for route generation.
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>
    /// Component parameter declarations
    /// </summary>
    public IReadOnlyList<ParameterDefinition>? Parameters { get; init; }

    /// <summary>
    /// Empty metadata instance (no YAML front matter)
    /// </summary>
    public static ComponentMetadata Empty { get; } = new();
}
