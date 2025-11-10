using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Markdn.SourceGenerators.Generators;

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

            // Get root namespace from compilation
            var rootNamespace = compilation.AssemblyName ?? "Generated";
            
            // For namespace generation, we'll use a simple heuristic
            // The file path typically contains project structure
            var projectRoot = GetProjectRootFromPath(file.Path);
            var namespaceValue = NamespaceGenerator.Generate(rootNamespace, file.Path, projectRoot);

            // For US1: Simple conversion - just treat content as markdown to be converted to HTML
            // We'll add Markdig parsing later
            var htmlContent = ConvertMarkdownToHtml(content);

            var source = GenerateComponentSource(
                componentName,
                namespaceValue,
                htmlContent);

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
        // US1: Simple conversion - for now just escape HTML and wrap in paragraph
        // We'll integrate Markdig properly in later tasks
        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var html = new StringBuilder();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Simple heading detection
            if (line.StartsWith("# "))
            {
                var text = System.Security.SecurityElement.Escape(line.Substring(2));
                html.AppendLine($"<h1>{text}</h1>");
            }
            else if (line.StartsWith("## "))
            {
                var text = System.Security.SecurityElement.Escape(line.Substring(3));
                html.AppendLine($"<h2>{text}</h2>");
            }
            else if (line.StartsWith("### "))
            {
                var text = System.Security.SecurityElement.Escape(line.Substring(4));
                html.AppendLine($"<h3>{text}</h3>");
            }
            else
            {
                var text = System.Security.SecurityElement.Escape(line);
                html.AppendLine($"<p>{text}</p>");
            }
        }

        return html.ToString();
    }

    private static string GenerateComponentSource(
        string componentName,
        string namespaceValue,
        string htmlContent)
    {
        return $@"// <auto-generated by Markdn.SourceGenerators v1.0.0 />
// This file is auto-generated. Do not edit directly.

#nullable enable

namespace {namespaceValue}
{{
    public partial class {componentName} : Microsoft.AspNetCore.Components.ComponentBase
    {{
        protected override void BuildRenderTree(
            Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {{
            builder.AddMarkupContent(0, @""{htmlContent.Replace("\"", "\"\"")}"");
        }}
    }}
}}
";
    }
}
