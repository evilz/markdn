using Microsoft.CodeAnalysis;

namespace Markdn.SourceGenerators.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the Markdown component generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "MarkdownGenerator";

    public static readonly DiagnosticDescriptor InvalidYamlFrontMatter = new(
        id: "MD102",
        title: "Invalid YAML front matter",
        messageFormat: "Invalid YAML front matter in '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidUrl = new(
        id: "MD002",
        title: "Invalid URL format",
        messageFormat: "URL must start with '/' in '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidParameterName = new(
        id: "MD085",
        title: "Invalid parameter name",
        messageFormat: "Parameter name '{0}' is not a valid C# identifier in '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidParameterType = new(
        id: "MD086",
        title: "Invalid parameter type",
        messageFormat: "Parameter type '{0}' is not valid C# type syntax in '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateParameterName = new(
        id: "MD087",
        title: "Duplicate parameter name",
        messageFormat: "Parameter name '{0}' is defined multiple times in '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnresolvableComponentReference = new(
        id: "MD006",
        title: "Component reference may not be resolvable",
        messageFormat: "Component '{0}' may not be resolvable in '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MalformedRazorSyntax = new(
        id: "MD103",
        title: "Malformed Razor syntax",
        messageFormat: "Malformed Razor syntax in '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MutuallyExclusiveUrls = new(
        id: "MD008",
        title: "Mutually exclusive URL properties",
        messageFormat: "Cannot specify both 'url' (string) and 'url' (array) in '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
