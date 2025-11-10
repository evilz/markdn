using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Markdn.SourceGenerators.Emitters;
using Markdn.SourceGenerators.Generators;
using Markdn.SourceGenerators.Models;
using Markdn.SourceGenerators.Parsers;

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

        // Combine with compilation to get root namespace
        var filesWithCompilation = markdownFiles
            .Combine(context.CompilationProvider);

        // Generate source for each markdown file
        context.RegisterSourceOutput(filesWithCompilation, GenerateSource);
    }

    private static void GenerateSource(
        SourceProductionContext context,
        (AdditionalText File, Compilation Compilation) input)
    {
        var (file, compilation) = input;
        var sourceText = file.GetText(context.CancellationToken);
        if (sourceText == null)
        {
            return;
        }

        try
        {
            var content = sourceText.ToString();
            var fileName = System.IO.Path.GetFileName(file.Path);
            var componentName = ComponentNameGenerator.Generate(fileName);

            // Parse YAML front matter and extract metadata
            var (metadata, markdownContent) = YamlFrontMatterParser.Parse(content);

            // Validate URL metadata (T044, T045)
            ValidateUrlMetadata(context, file, metadata);

            // Preserve Razor syntax before Markdown processing
            var razorPreserver = new RazorPreserver();
            var contentWithPlaceholders = razorPreserver.ExtractRazorSyntax(markdownContent);

            // Get root namespace from compilation
            var rootNamespace = compilation.AssemblyName ?? "Generated";
            
            // For namespace generation, we'll use a simple heuristic
            // The file path typically contains project structure
            var projectRoot = GetProjectRootFromPath(file.Path);
            var namespaceValue = metadata.Namespace ?? NamespaceGenerator.Generate(rootNamespace, file.Path, projectRoot);

            // Convert Markdown content to HTML (with placeholders)
            var htmlWithPlaceholders = ConvertMarkdownToHtml(contentWithPlaceholders);

            // Restore Razor syntax after Markdown processing, but exclude @code blocks
            // @code blocks are emitted separately, not in the BuildRenderTree
            var htmlContent = razorPreserver.RestoreRazorSyntax(htmlWithPlaceholders, excludeCodeBlocks: true);

            // Extract @code blocks for separate emission (T053-T055)
            var codeBlocks = ExtractCodeBlocks(razorPreserver);

            var source = ComponentCodeEmitter.Emit(
                componentName,
                namespaceValue,
                htmlContent,
                metadata,
                codeBlocks);

            context.AddSource(
                $"{componentName}.md.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            // Report any errors as diagnostics
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "MD999",
                    "Generator error",
                    $"Error generating component: {ex.Message}",
                    "MarkdownGenerator",
                    DiagnosticSeverity.Error,
                    true),
                Location.None);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string GetProjectRootFromPath(string filePath)
    {
        // Source generators can't use File I/O, so we use path heuristics
        // Typically paths look like: C:\path\to\project\Pages\File.md
        // We look for common root directories (Pages, Components, etc.)
        var directory = System.IO.Path.GetDirectoryName(filePath) ?? filePath;
        
        // Check if path contains common component directories
        var commonDirs = new[] { "Pages", "Components", "Shared", "Views" };
        foreach (var dir in commonDirs)
        {
            var index = directory.LastIndexOf(System.IO.Path.DirectorySeparatorChar + dir);
            if (index > 0)
            {
                return directory.Substring(0, index);
            }
        }
        
        // Fallback: use parent directory of file
        return directory;
    }

    private static string ConvertMarkdownToHtml(string markdown)
    {
        // Use basic parser due to source generator assembly isolation limitations
        // Markdig integration deferred until runtime preprocessing solution available
        return BasicMarkdownParser.ConvertToHtml(markdown);
    }

    private static List<CodeBlock> ExtractCodeBlocks(RazorPreserver preserver)
    {
        // T053-T055: Extract @code blocks from preserved content
        var codeBlocks = new List<CodeBlock>();
        
        // Get preserved blocks from the RazorPreserver
        var preservedBlocks = preserver.GetPreservedBlocks();
        
        foreach (var kvp in preservedBlocks)
        {
            var placeholder = kvp.Key;
            var content = kvp.Value;
            
            // Check if this is a @code block
            if (content.StartsWith("@code"))
            {
                // Extract code content (remove @code { and closing })
                var codeContent = ExtractCodeBlockContent(content);
                
                codeBlocks.Add(new CodeBlock
                {
                    Content = codeContent,
                    Location = null // Location tracking can be added later
                });
            }
        }
        
        return codeBlocks;
    }

    private static string ExtractCodeBlockContent(string codeBlock)
    {
        // Remove "@code {" from start and "}" from end
        var content = codeBlock.Trim();
        
        // Find the opening brace
        var openBraceIndex = content.IndexOf('{');
        if (openBraceIndex == -1)
            return string.Empty;
            
        // Extract content between braces
        var startIndex = openBraceIndex + 1;
        var endIndex = content.LastIndexOf('}');
        
        if (endIndex <= startIndex)
            return string.Empty;
            
        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static void ValidateUrlMetadata(
        SourceProductionContext context,
        AdditionalText file,
        ComponentMetadata metadata)
    {
        var fileName = System.IO.Path.GetFileName(file.Path);

        // T044: Check if both Url and UrlArray are specified (mutually exclusive)
        if (!string.IsNullOrEmpty(metadata.Url) && metadata.UrlArray != null && metadata.UrlArray.Count > 0)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.DiagnosticDescriptors.MutuallyExclusiveUrls,
                Location.None,
                fileName);
            context.ReportDiagnostic(diagnostic);
        }

        // T045: Validate that single URL starts with /
        if (!string.IsNullOrEmpty(metadata.Url) && !metadata.Url.StartsWith("/"))
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.DiagnosticDescriptors.InvalidUrl,
                Location.None,
                fileName,
                metadata.Url);
            context.ReportDiagnostic(diagnostic);
        }

        // T045: Validate that all URLs in array start with /
        if (metadata.UrlArray != null)
        {
            foreach (var url in metadata.UrlArray)
            {
                if (!url.StartsWith("/"))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.InvalidUrl,
                        Location.None,
                        fileName,
                        url);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
