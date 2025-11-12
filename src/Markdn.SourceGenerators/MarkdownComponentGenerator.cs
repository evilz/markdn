using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        // Read build property RootNamespace (if provided) from analyzer config options.
        var rootNamespaceProvider = context.AnalyzerConfigOptionsProvider.Select((opts, ct) =>
        {
            if (opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var val))
            {
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
            return null;
        });

        // Combine files, compilation and root-namespace option
        var combined = filesWithCompilation.Combine(rootNamespaceProvider);

        // Generate source for each markdown file with knowledge of the project's RootNamespace
        // Use an inline lambda to destructure the combined tuple to avoid complex
        // tuple type signatures in the method declaration.
        context.RegisterSourceOutput(combined, (spc, input) =>
        {
            try
            {
                var ((file, compilation), providedRootNamespace) = input;
                GenerateSourceWithOptions(spc, file, compilation, providedRootNamespace);
            }
            catch (Exception ex)
            {
                var diag = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "MD999",
                        "Generator error",
                        $"Error during source generation dispatch: {ex.Message}",
                        "MarkdownGenerator",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None);
                spc.ReportDiagnostic(diag);
            }
        });
    }

    private static void GenerateSourceWithOptions(
        SourceProductionContext context,
        AdditionalText file,
        Compilation compilation,
        string? providedRootNamespace)
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
            var componentName = ComponentNameGenerator.Generate(fileName);

            // Parse YAML front matter and extract metadata
            var (metadata, markdownContent, yamlErrors) = YamlFrontMatterParser.Parse(content);

            // Report YAML parsing errors (T102)
            foreach (var error in yamlErrors)
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.InvalidYamlFrontMatter,
                    Location.None,
                    fileName,
                    error);
                context.ReportDiagnostic(diagnostic);
            }

            // Validate URL metadata (T044, T045)
            ValidateUrlMetadata(context, file, metadata);

            // Validate parameter metadata (T085, T086, T087)
            ValidateParameterMetadata(context, file, metadata);

            // Preserve Razor syntax before Markdown processing
            var razorPreserver = new RazorPreserver();
            var contentWithPlaceholders = razorPreserver.ExtractRazorSyntax(markdownContent);

            // T104: Surface parser-level component tag concerns early.
            // The RazorPreserver collects component tag names it found; report a low-confidence
            // MD006 (UnresolvableComponentReference) for names that are not valid C# identifiers
            // (this helps catch obvious user mistakes like invalid characters or hyphens).
            // Filter out empty/whitespace names up-front to keep the loop body focused.
            var parserComponentNames = razorPreserver.GetComponentNames()
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.Trim());

            foreach (var compName in parserComponentNames)
            {
                // If the name is not a valid C# identifier, warn early
                if (!Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier(compName))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.UnresolvableComponentReference,
                        Location.None,
                        compName,
                        fileName);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Report any Razor preservation/parsing errors (T103)
            var razorErrors = razorPreserver.GetErrors();
            foreach (var err in razorErrors)
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.MalformedRazorSyntax,
                    Location.None,
                    fileName,
                    err);
                context.ReportDiagnostic(diagnostic);
            }

            // Prefer the project's RootNamespace (from MSBuild) when available; fall back to assembly name
            var rootNamespace = !string.IsNullOrWhiteSpace(providedRootNamespace)
                ? providedRootNamespace!
                : (compilation.AssemblyName ?? "Generated");
            
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

            // Attempt to resolve referenced component types in the current compilation so
            // we can emit fully-qualified type names (avoids needing fragile using directives).
            // Strategy:
            // 1) Scan the HTML for component tag simple names.
            // 2) For each name, first try to find a type in this compilation by simple name
            //    (FindTypeNamespaceBySimpleName). If found and it's a component, emit the
            //    fully-qualified namespace for that type.
            // 3) If not found, respect explicit front-matter `componentNamespaces` by trying
            //    to resolve the type using those namespaces.
            // 4) If still unresolved, try common candidate namespaces under the project's
            //    root namespace as a last attempt.
            // 5) If unresolved after all attempts, emit MD006 to clearly inform the user.

            var componentTypeMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var componentNamePattern = new System.Text.RegularExpressions.Regex(@"<([A-Z][A-Za-z0-9_]*)\b");
            var matches = componentNamePattern.Matches(htmlContent);
            var componentNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var name = m.Groups[1].Value;
                if (string.IsNullOrEmpty(name) || componentNames.Contains(name))
                {
                    continue;
                }
                componentNames.Add(name);

                // 1) Try to find any type in the compilation with this simple name (preferred)
                var discoveredNs = FindTypeNamespaceBySimpleName(compilation, name);
                if (!string.IsNullOrEmpty(discoveredNs))
                {
                    componentTypeMap[name] = discoveredNs;
                    continue;
                }

                // 2) If front-matter provides explicit component namespaces, attempt to resolve
                if (metadata.ComponentNamespaces != null && metadata.ComponentNamespaces.Count > 0)
                {
                    foreach (var ns in metadata.ComponentNamespaces)
                    {
                        if (string.IsNullOrWhiteSpace(ns))
                        {
                            continue;
                        }

                        var full = ns.Trim().TrimEnd('.') + "." + name;
                        var symbol = compilation.GetTypeByMetadataName(full);
                        if (symbol != null && IsComponentType(symbol))
                        {
                            componentTypeMap[name] = symbol.ContainingNamespace?.ToDisplayString() ?? ns.Trim();
                            break;
                        }
                    }

                    // If type wasn't found in compilation but namespace is explicitly provided,
                    // trust it and add to the map anyway. This handles Razor components that
                    // haven't been compiled yet when the source generator runs.
                    if (!componentTypeMap.ContainsKey(name))
                    {
                        // Use the first provided namespace as the resolution
                        var firstNs = metadata.ComponentNamespaces.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                        if (!string.IsNullOrEmpty(firstNs))
                        {
                            componentTypeMap[name] = firstNs.Trim().TrimEnd('.');
                        }
                    }

                    if (componentTypeMap.ContainsKey(name))
                        continue;
                }

                // 3) Try common candidate namespaces under the project's root namespace
                var candidates = new[] {
                    $"{rootNamespace}.Components.{name}",
                    $"{rootNamespace}.Components.Shared.{name}",
                    $"{rootNamespace}.Components.Pages.{name}",
                    $"{rootNamespace}.Pages.{name}",
                    $"{rootNamespace}.{name}"
                };

                foreach (var full in candidates)
                {
                    var symbol = compilation.GetTypeByMetadataName(full);
                    if (symbol != null && IsComponentType(symbol))
                    {
                        var ns = symbol.ContainingNamespace?.ToDisplayString();
                        if (!string.IsNullOrEmpty(ns))
                        {
                            componentTypeMap[name] = ns;
                            break;
                        }
                    }
                }

                // 4) If still unresolved, report a diagnostic so the user knows how to fix it
                if (!componentTypeMap.ContainsKey(name))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.UnresolvableComponentReference,
                        Location.None,
                        name,
                        fileName);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Determine which candidate namespaces actually exist in the compilation
            List<string> availableNamespaces;
            if (metadata.ComponentNamespaces != null && metadata.ComponentNamespaces.Count > 0)
            {
                // Use explicit namespaces provided in front-matter
                availableNamespaces = metadata.ComponentNamespaces.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList();
            }
            else
            {
                var namespaceCandidates = new[] {
                    rootNamespace + ".Components",
                    rootNamespace + ".Components.Shared",
                    rootNamespace + ".Components.Pages",
                    rootNamespace + ".Pages",
                    rootNamespace
                };

                availableNamespaces = new List<string>();
                foreach (var nsCandidate in namespaceCandidates)
                {
                    if (NamespaceExists(compilation, nsCandidate))
                    {
                        availableNamespaces.Add(nsCandidate);
                    }
                }
            }

            var source = ComponentCodeEmitter.Emit(
                componentName,
                namespaceValue,
                htmlContent,
                metadata,
                codeBlocks,
                componentTypeMap,
                availableNamespaces);

            // Create unique hint name using relative path from project root
            var relativePath = GetRelativePath(file.Path, projectRoot);
            var hintName = relativePath.Replace(System.IO.Path.DirectorySeparatorChar, '_')
                                      .Replace(System.IO.Path.AltDirectorySeparatorChar, '_')
                                      .Replace(".md", ".md.g.cs");

            context.AddSource(
                hintName,
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

    private static string GetRelativePath(string filePath, string projectRoot)
    {
        // Remove project root from file path to get relative path
        if (filePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = filePath.Substring(projectRoot.Length);
            if (relativePath.StartsWith(System.IO.Path.DirectorySeparatorChar.ToString()) ||
                relativePath.StartsWith(System.IO.Path.AltDirectorySeparatorChar.ToString()))
            {
                relativePath = relativePath.Substring(1);
            }
            return relativePath;
        }
        
        // Fallback: use filename
        return System.IO.Path.GetFileName(filePath) ?? "unknown.md";
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

    private static bool IsValidGenericType(string typeName)
    {
        // Basic check for generic types like List<string>, Dictionary<string, int>
        // This is a simple heuristic - not a full C# type parser
        if (!typeName.Contains("<") || !typeName.Contains(">"))
        {
            return false;
        }
        
        // Count brackets to ensure they're balanced
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
            // T085: Validate parameter name is valid C# identifier
            if (!SyntaxFacts.IsValidIdentifier(parameter.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.InvalidParameterName,
                    Location.None,
                    parameter.Name,
                    fileName);
                context.ReportDiagnostic(diagnostic);
            }

            // T087: Check for duplicate parameter names
            if (!seenNames.Add(parameter.Name))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.DuplicateParameterName,
                    Location.None,
                    parameter.Name,
                    fileName);
                context.ReportDiagnostic(diagnostic);
            }

            // T086: Validate parameter type is valid C# type syntax
            // For now, do basic validation - check it's not empty and contains valid characters
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
                // Basic validation: check for common invalid patterns
                var trimmedType = parameter.Type.Trim();
                
                // Check for spaces (types shouldn't have spaces unless generic)
                if (trimmedType.Contains(" ") && !IsValidGenericType(trimmedType))
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.InvalidParameterType,
                        Location.None,
                        parameter.Type,
                        fileName);
                    context.ReportDiagnostic(diagnostic);
                }
                // Check for other invalid characters
                else if (trimmedType.Contains("<<") || trimmedType.Contains(">>") || 
                    trimmedType.Contains("&&&") || trimmedType.Contains("|||") ||
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
                // Check for starting with number
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

    private static string? FindTypeNamespaceBySimpleName(Compilation compilation, string simpleName)
    {
        try
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(compilation.GlobalNamespace);
            while (stack.Count > 0)
            {
                var ns = stack.Pop();
                foreach (var member in ns.GetTypeMembers())
                {
                    if (member.Name == simpleName)
                    {
                        if (IsComponentType(member))
                        {
                            return member.ContainingNamespace?.ToDisplayString();
                        }
                    }
                }
                foreach (var child in ns.GetNamespaceMembers())
                {
                    stack.Push(child);
                }
            }
        }
        catch
        {
            // best-effort: swallow and return null
        }
        return null;
    }

    private static bool IsComponentType(INamedTypeSymbol symbol)
    {
        try
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                var fullName = baseType.ToDisplayString();
                if (fullName == "Microsoft.AspNetCore.Components.ComponentBase" || fullName.EndsWith(".ComponentBase"))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
        }
        catch
        {
            // ignore and return false
        }
        return false;
    }

    private static bool NamespaceExists(Compilation compilation, string namespaceName)
    {
        var ns = FindNamespaceSymbol(compilation.GlobalNamespace, namespaceName);
        if (ns == null)
            return false;

        // Consider a namespace to "exist" for our purposes only if it contains any type members
        // (placeholder/empty namespace declarations without types should not count).
        return NamespaceHasAnyType(ns);
    }

    private static bool NamespaceHasAnyType(INamespaceSymbol ns)
    {
        try
        {
            if (ns.GetTypeMembers().Length > 0)
                return true;

            foreach (var child in ns.GetNamespaceMembers())
            {
                if (NamespaceHasAnyType(child))
                    return true;
            }
        }
        catch
        {
            // swallow and fall through
        }
        return false;
    }

    private static INamespaceSymbol? FindNamespaceSymbol(INamespaceSymbol ns, string target)
    {
        if (ns.ToDisplayString() == target)
        {
            return ns;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = FindNamespaceSymbol(child, target);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
