using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Markdn.SourceGenerators.Models;
using Markdn.SourceGenerators.Parsers;
using Markdig;

namespace Markdn.SourceGenerators;

/// <summary>
/// Incremental source generator that converts Markdown files to Blazor Razor components.
/// </summary>
[Generator]
public class MarkdownComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter for .md files
        var markdownFiles = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

        // Generate source for each markdown file
        context.RegisterSourceOutput(markdownFiles, (spc, file) =>
        {
            try
            {
                GenerateRazorComponent(spc, file);
            }
            catch (Exception ex)
            {
                var diag = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "MD999",
                        "Generator error",
                        $"Error during source generation: {ex.Message}",
                        "MarkdownGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None);
                spc.ReportDiagnostic(diag);
            }
        });
    }

    private static void GenerateRazorComponent(SourceProductionContext context, AdditionalText file)
    {
        var sourceText = file.GetText(context.CancellationToken);
        if (sourceText == null)
        {
            return;
        }

        try
        {
            var content = sourceText.ToString();
            var fileName = System.IO.Path.GetFileName(file.Path);

            // Step 1: Parse YAML front matter using Markdig (via YamlFrontMatterParser)
            var (metadata, markdownContent, yamlErrors) = YamlFrontMatterParser.Parse(content);

            // Report YAML parsing errors
            foreach (var error in yamlErrors)
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.InvalidYamlFrontMatter,
                    Location.None,
                    fileName,
                    error);
                context.ReportDiagnostic(diagnostic);
            }

            // Validate parameter metadata
            ValidateParameterMetadata(context, file, metadata);

            // Step 2: Determine slug route
            // If slug is supplied => use it like @page "{slug}"
            // else use the file path to generate slug
            var slugRoute = ResolveSlugRoute(metadata, file.Path);

            // Step 3: Parse Markdown to HTML using Markdig
            var pipeline = MarkdigPipelineBuilder.Build();
            var htmlContent = Markdown.ToHtml(markdownContent, pipeline);

            // Step 4: Generate Razor file content
            var razorContent = GenerateRazorFile(metadata, slugRoute, htmlContent);

            // Step 5: Compute hint name and add source
            var hintName = ComputeHintName(file.Path);
            context.AddSource(
                hintName,
                SourceText.From(razorContent, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "MD999",
                    "Generator error",
                    $"Error generating component for '{file.Path}': {ex.Message}",
                    "MarkdownGenerator",
                    DiagnosticSeverity.Error,
                    true),
                Location.None);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static readonly string[] _routePrefixDirectories = new[] { "pages", "components", "shared", "views", "content" };

    /// <summary>
    /// Resolve the slug route from metadata or file path
    /// </summary>
    private static string? ResolveSlugRoute(ComponentMetadata metadata, string filePath)
    {
        // If slug is supplied in front matter, use it
        if (!string.IsNullOrWhiteSpace(metadata.Slug))
        {
            return NormalizeSlugRoute(metadata.Slug!);
        }

        // Otherwise, derive from file path
        return DeriveSlugFromPath(filePath);
    }

    /// <summary>
    /// Derive slug from file path by removing known prefix directories
    /// </summary>
    private static string DeriveSlugFromPath(string filePath)
    {
        var path = filePath.Replace('\\', '/');
        var lower = path.ToLowerInvariant();

        int start = 0;
        foreach (var dir in _routePrefixDirectories)
        {
            var needle = "/" + dir + "/";
            var idx = lower.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                start = idx + needle.Length;
                break;
            }

            if (lower.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase))
            {
                start = dir.Length + 1;
                break;
            }
        }

        var relative = path.Substring(start);
        if (relative.StartsWith("/"))
        {
            relative = relative.Substring(1);
        }

        if (relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            relative = relative.Substring(0, relative.Length - 3);
        }

        relative = relative.Trim('/');
        if (string.IsNullOrEmpty(relative))
        {
            return "/";
        }

        return "/" + relative.ToLowerInvariant();
    }

    /// <summary>
    /// Normalize slug route to ensure it starts with / and is lowercase
    /// </summary>
    private static string NormalizeSlugRoute(string slug)
    {
        var normalized = slug.Replace('\\', '/').Trim();

        if (normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - 3);
        }

        // Remove known folder prefixes if they're at the start
        foreach (var prefix in _routePrefixDirectories)
        {
            if (normalized.StartsWith("/" + prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length + 1);
                break;
            }
            if (normalized.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length);
                break;
            }
        }

        normalized = normalized.Trim('/');
        if (string.IsNullOrEmpty(normalized))
        {
            return "/";
        }

        if (!normalized.StartsWith("/"))
        {
            normalized = "/" + normalized;
        }

        return normalized.ToLowerInvariant();
    }

    /// <summary>
    /// Generate Razor file content from metadata and HTML
    /// </summary>
    private static string GenerateRazorFile(ComponentMetadata metadata, string? slugRoute, string htmlContent)
    {
        var sb = new StringBuilder();

        // Add @page directive if route is available
        if (!string.IsNullOrWhiteSpace(slugRoute))
        {
            sb.AppendLine($"@page \"{slugRoute}\"");
            sb.AppendLine();
        }

        // Add @using directives from metadata
        if (metadata.Using != null && metadata.Using.Count > 0)
        {
            foreach (var usingDirective in metadata.Using)
            {
                sb.AppendLine($"@using {usingDirective}");
            }
            sb.AppendLine();
        }

        // Add layout attribute if specified
        if (!string.IsNullOrEmpty(metadata.Layout))
        {
            sb.AppendLine($"@layout {metadata.Layout}");
            sb.AppendLine();
        }

        // Add inherit directive if specified
        if (!string.IsNullOrEmpty(metadata.Inherit))
        {
            sb.AppendLine($"@inherits {metadata.Inherit}");
            sb.AppendLine();
        }

        // Add custom attributes if specified
        if (metadata.Attribute != null && metadata.Attribute.Count > 0)
        {
            foreach (var attribute in metadata.Attribute)
            {
                sb.AppendLine($"@attribute [{attribute}]");
            }
            sb.AppendLine();
        }

        // Add PageTitle component if title is specified
        if (!string.IsNullOrEmpty(metadata.Title))
        {
            sb.AppendLine($"<PageTitle>{EscapeHtml(metadata.Title!)}</PageTitle>");
            sb.AppendLine();
        }

        // Add the HTML content
        sb.AppendLine(htmlContent);

        // Add @code block with parameters if specified
        if (metadata.Parameters != null && metadata.Parameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("@code {");

            foreach (var parameter in metadata.Parameters)
            {
                sb.AppendLine("    [Parameter]");
                var defaultValue = IsReferenceType(parameter.Type) ? " = default!;" : "";
                sb.AppendLine($"    public {parameter.Type} {parameter.Name} {{ get; set; }}{defaultValue}");
                sb.AppendLine();
            }

            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escape HTML special characters
    /// </summary>
    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;");
    }

    /// <summary>
    /// Check if a type is a reference type (needs = default! initialization)
    /// </summary>
    private static bool IsReferenceType(string typeName)
    {
        var valueTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint",
            "long", "ulong", "short", "ushort", "nint", "nuint"
        };

        if (valueTypes.Contains(typeName))
        {
            return false;
        }

        if (typeName.EndsWith("?"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Compute hint name for generated file
    /// </summary>
    private static string ComputeHintName(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "generated.md";
        }

        // Use simple string replacement for .NET Standard 2.0 compatibility
        if (fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return fileName.Substring(0, fileName.Length - 3) + ".razor.g";
        }

        return fileName + ".razor.g";
    }

    /// <summary>
    /// Validate parameter metadata and report diagnostics
    /// </summary>
    private static void ValidateParameterMetadata(
        SourceProductionContext context,
        AdditionalText file,
        ComponentMetadata metadata)
    {
        var fileName = System.IO.Path.GetFileName(file.Path);

        if (metadata.Parameters == null || metadata.Parameters.Count == 0)
        {
            return;
        }

        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in metadata.Parameters)
        {
            // Validate parameter name is valid C# identifier
            if (!SyntaxFacts.IsValidIdentifier(parameter.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.InvalidParameterName,
                    Location.None,
                    parameter.Name,
                    fileName);
                context.ReportDiagnostic(diagnostic);
            }

            // Check for duplicate parameter names
            if (!seenNames.Add(parameter.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.DuplicateParameterName,
                    Location.None,
                    parameter.Name,
                    fileName);
                context.ReportDiagnostic(diagnostic);
            }

            // Validate parameter type
            if (string.IsNullOrWhiteSpace(parameter.Type))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.InvalidParameterType,
                    Location.None,
                    parameter.Type,
                    fileName);
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                var trimmedType = parameter.Type.Trim();

                if (trimmedType.Contains(" ") && !IsValidGenericType(trimmedType))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.InvalidParameterType,
                        Location.None,
                        parameter.Type,
                        fileName);
                    context.ReportDiagnostic(diagnostic);
                }
                else if (trimmedType.Contains("<<") || trimmedType.Contains(">>") ||
                    trimmedType.StartsWith(".") || trimmedType.EndsWith(".") ||
                    trimmedType.Contains(".."))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.InvalidParameterType,
                        Location.None,
                        parameter.Type,
                        fileName);
                    context.ReportDiagnostic(diagnostic);
                }
                else if (trimmedType.Length > 0 && char.IsDigit(trimmedType[0]))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.InvalidParameterType,
                        Location.None,
                        parameter.Type,
                        fileName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    /// <summary>
    /// Check if a type name represents a valid generic type
    /// </summary>
    private static bool IsValidGenericType(string typeName)
    {
        if (!typeName.Contains("<") || !typeName.Contains(">"))
        {
            return false;
        }

        int angleBracketCount = 0;
        foreach (char c in typeName)
        {
            if (c == '<')
            {
                angleBracketCount++;
            }
            else if (c == '>')
            {
                angleBracketCount--;
            }
            if (angleBracketCount < 0)
            {
                return false;
            }
        }
        return angleBracketCount == 0;
    }
}

