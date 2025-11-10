using System.Collections.Generic;

namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Strongly-typed representation of YAML front matter configuration.
/// </summary>
public sealed class ComponentMetadata
{
    /// <summary>
    /// Single route URL (e.g., "/home"). Mutually exclusive with UrlArray.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Multiple route URLs (e.g., ["/", "/home"]). Mutually exclusive with Url.
    /// </summary>
    public IReadOnlyList<string>? UrlArray { get; init; }

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
    /// Component parameter declarations
    /// </summary>
    public IReadOnlyList<ParameterDefinition>? Parameters { get; init; }

    /// <summary>
    /// Empty metadata instance (no YAML front matter)
    /// </summary>
    public static ComponentMetadata Empty { get; } = new();
}
