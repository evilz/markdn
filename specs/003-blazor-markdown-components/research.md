# Research: Markdown to Razor Component Generator

**Feature**: 003-blazor-markdown-components  
**Date**: 2025-11-10  
**Status**: Complete

## Overview

This document consolidates research findings for implementing an MDX-like Markdown to Razor Component Generator for Blazor using C# incremental source generators.

## Technical Decisions

### 1. Source Generator Architecture

**Decision**: Implement as C# Incremental Source Generator using `IIncrementalGenerator`

**Rationale**:
- Incremental generators only regenerate changed files (performance)
- Integration with Roslyn compilation pipeline provides:
  - IntelliSense support for generated components
  - Compile-time validation of generated code
  - Seamless hot reload integration
- No runtime overhead (all work done at compile time)
- Standard .NET source generator distribution model

**Alternatives Considered**:
- **MSBuild Task**: Would work but lacks IntelliSense integration, harder to debug
- **T4 Templates**: Legacy technology, poor developer experience, no incremental support
- **Runtime Code Generation**: Would add runtime overhead, complexity, and security concerns

**Implementation Pattern**:
```csharp
[Generator]
public class MarkdownComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register for .md files
        var markdownFiles = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
        
        // Transform to component models
        var componentModels = markdownFiles.Select(TransformToModel);
        
        // Generate source code
        context.RegisterSourceOutput(componentModels, GenerateComponentSource);
    }
}
```

**References**:
- [Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Incremental Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)

---

### 2. Markdig Custom Pipeline for Razor Preservation

**Decision**: Extend Markdig with custom inline parser that detects and preserves Razor syntax

**Rationale**:
- Markdig is already in the project (reuse existing dependency)
- Highly extensible architecture with pipeline configuration
- Supports custom inline parsers for special syntax
- Mature, well-tested, supports CommonMark + GFM extensions

**Implementation Approach**:
- Create `RazorSyntaxPreserver` implementing Markdig's `IInlineParser`
- Detect patterns: `@identifier`, `@(expression)`, `@code { }`, `<Component />`
- Emit custom `RazorInline` AST nodes that render as-is without escaping
- Register in pipeline before default HTML rendering

**Alternatives Considered**:
- **Pre-process Markdown**: Replace Razor syntax with placeholders → More fragile, loses context
- **Post-process HTML**: Find escaped Razor syntax and unescape → Unreliable, can't distinguish from literal content
- **Regex replacement**: Brittle, doesn't understand Markdown context

**Code Structure**:
```csharp
public class RazorSyntaxPreserver : InlineParser
{
    // Patterns: @ followed by identifier, (expression), code block, or component tag
    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        if (slice.CurrentChar == '@')
        {
            // Parse @identifier, @(expr), @code { }
            // Create RazorInlineNode with preserved text
            return true;
        }
        if (slice.CurrentChar == '<' && IsComponentTag(slice))
        {
            // Parse <ComponentName .../>
            // Create ComponentInlineNode with preserved text
            return true;
        }
        return false;
    }
}
```

**References**:
- [Markdig Extensibility](https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/CustomContainerExtensionTests.md)
- [Markdig Custom Parsers](https://github.com/xoofx/markdig/tree/master/src/Markdig/Parsers)

---

### 3. YAML Front Matter Parsing

**Decision**: Use YamlDotNet (already in project) with schema validation

**Rationale**:
- YamlDotNet already present for content collections feature
- Mature library with good error messages
- Supports deserializing to strongly-typed models
- Schema validation available via custom deserializers

**Implementation Pattern**:
```csharp
public class ComponentMetadata
{
    public string? Url { get; set; }
    public string[]? UrlArray { get; set; } // For multiple routes
    public string? Title { get; set; }
    public string? Namespace { get; set; }
    public string[]? Using { get; set; }
    public string? Layout { get; set; }
    public string? Inherit { get; set; }
    public string[]? Attribute { get; set; }
    public ParameterDefinition[]? Parameters { get; set; }
}

public class ParameterDefinition
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
}
```

**Validation Strategy**:
- Required fields: validate in model property setters
- Type syntax: validate Type field is valid C# type identifier
- Emit diagnostic if invalid (e.g., "Parameter Type must be a valid C# type")

**Alternatives Considered**:
- **JSON front matter**: Less human-readable, not standard for Markdown metadata
- **Custom parser**: Reinventing the wheel, error-prone

---

### 4. Component Code Generation Strategy

**Decision**: Generate complete ComponentBase-derived class with BuildRenderTree method

**Rationale**:
- `BuildRenderTree` is Blazor's rendering API - full control over output
- Can mix static HTML (from Markdown) with dynamic rendering (Razor expressions)
- Supports all Blazor features: parameters, lifecycle, child content
- Generated code is human-readable for debugging

**Generated Code Pattern**:
```csharp
// Home.md.g.cs
namespace MyApp.Pages
{
    [Route("/")]
    public partial class Home : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, "Hello, World!");
            builder.CloseElement();
            
            // For Razor expression @DateTime.Now:
            builder.AddContent(2, DateTime.Now);
            
            // For component reference <Counter />:
            builder.OpenComponent<Counter>(3);
            builder.CloseComponent();
        }
    }
}
```

**For @code blocks**: Extract and emit as class members (methods, fields, properties)

**For parameters**: Generate [Parameter] properties before BuildRenderTree

**Alternatives Considered**:
- **Generate .razor files**: Would work but requires Razor compiler, loses source generator benefits (IntelliSense, debugging)
- **Markup strings**: Less efficient, no compile-time validation
- **Razor syntax trees**: Overly complex for our needs

**References**:
- [BuildRenderTree Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase.buildrenderttree)
- [RenderTreeBuilder API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.rendering.rendertreebuilder)

---

### 5. Hot Reload Support

**Decision**: Hot reload works automatically via incremental source generator + Blazor's built-in hot reload

**Rationale**:
- Incremental generators re-run when input files (.md) change
- Blazor hot reload detects generated .cs file changes
- No additional infrastructure needed

**Mechanism**:
1. Developer edits `Home.md`
2. File watcher triggers incremental generator
3. Generator emits updated `Home.md.g.cs`
4. Blazor hot reload detects source change
5. Browser updates without full restart

**State Preservation**: Blazor's hot reload automatically preserves component state when only rendering changes (not parameter/lifecycle changes)

**Limitations**: 
- Adding/removing parameters requires full restart (Blazor limitation)
- Changes to @code blocks that affect component signature require full restart

**No Additional Implementation Required**: Leverage existing infrastructure

---

### 6. Namespace Generation from Directory Structure

**Decision**: Map folder hierarchy to namespace hierarchy

**Rationale**:
- Intuitive developer experience (matches existing .cs file conventions)
- Enables logical organization of components
- Standard .NET practice

**Algorithm**:
```csharp
string GenerateNamespace(string mdFilePath, string projectRootNamespace)
{
    // Example: Pages/Blog/Post.md → MyApp.Pages.Blog
    var relativePath = Path.GetRelativePath(projectRoot, Path.GetDirectoryName(mdFilePath));
    var namespaceParts = relativePath.Split(Path.DirectorySeparatorChar)
        .Where(p => !string.IsNullOrEmpty(p) && p != ".")
        .Select(SanitizeNamespacePart);
    
    return $"{projectRootNamespace}.{string.Join(".", namespaceParts)}";
}
```

**Override**: Respect `$namespace` in YAML front matter if specified

---

### 7. Error Handling Strategy

**Decision**: Use Roslyn's diagnostic reporting for all errors and warnings

**Rationale**:
- Integrates with IDE (squiggly lines, error list)
- Standard .NET build error format
- Supports severity levels (error, warning, info)
- Can include source location for precise error reporting

**Diagnostic Categories**:
- **MD001**: Invalid YAML front matter syntax
- **MD002**: Invalid parameter type in $parameters
- **MD003**: Unsupported YAML front matter key
- **MD004**: Malformed Razor syntax in Markdown
- **MD005**: Component reference not found
- **MD006**: Duplicate route URL across files

**Reporting Pattern**:
```csharp
context.ReportDiagnostic(Diagnostic.Create(
    new DiagnosticDescriptor(
        id: "MD001",
        title: "Invalid YAML front matter",
        messageFormat: "Failed to parse YAML: {0}",
        category: "MarkdownGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true),
    location: Location.None, // Or specific location if available
    "Unexpected token on line 3"));
```

---

### 8. Testing Strategy

**Decision**: Three-tier testing approach

**Unit Tests** (Markdn.SourceGenerators.Tests):
- YAML parser tests (valid/invalid input)
- Markdown parser tests (Razor preservation)
- Code emitter tests (verify generated C# syntax)
- Parameter generation tests
- Namespace generation tests

**Generator Tests** (using Microsoft.CodeAnalysis.Testing):
- End-to-end: `.md` input → generated `.cs` output verification
- Compilation tests: verify generated code compiles
- Verify IntelliSense works on generated components

**Integration Tests** (Markdn.Blazor.App.Tests):
- Render generated components in test host
- Verify HTML output matches expected
- Verify parameters pass correctly
- Verify component composition works

**Test Tools**:
- xUnit + FluentAssertions
- Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing for generator tests
- bUnit for Blazor component rendering tests

---

### 9. Project Integration (No NuGet)

**Decision**: In-solution project reference with explicit MSBuild configuration

**Rationale**:
- User requirement: no external NuGet package
- Allows developers to modify generator for their needs
- Simpler debugging during development

**Project Configuration**:

**Markdn.SourceGenerators.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <!-- Reference existing Markdig/YamlDotNet packages -->
  </ItemGroup>
</Project>
```

**Markdn.Blazor.App.csproj**:
```xml
<ItemGroup>
  <!-- Reference as source generator -->
  <ProjectReference Include="..\Markdn.SourceGenerators\Markdn.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  
  <!-- Include all .md files as additional files for generator -->
  <AdditionalFiles Include="**\*.md" />
</ItemGroup>
```

**Key Point**: `OutputItemType="Analyzer"` treats project as source generator rather than normal reference

---

### 10. Component Naming Convention

**Decision**: Component class name = Markdown filename (without extension)

**Rationale**:
- Simple, predictable mapping
- Matches existing Razor component conventions
- Easy to use: `<Home />` for `Home.md`

**Handling Special Cases**:
- Invalid C# identifiers (e.g., `my-page.md`): Convert to PascalCase (`MyPage`)
- Numeric prefixes (e.g., `2024-post.md`): Prepend underscore (`_2024Post`) or strip date prefix
- Reserved keywords (e.g., `class.md`): Prepend @ (`@class` class name)

**Algorithm**:
```csharp
string GenerateClassName(string filename)
{
    var name = Path.GetFileNameWithoutExtension(filename);
    
    // Remove date prefix if present (YYYY-MM-DD-)
    var datePattern = new Regex(@"^\d{4}-\d{2}-\d{2}-");
    name = datePattern.Replace(name, "");
    
    // Convert kebab-case to PascalCase
    name = string.Join("", name.Split('-', '_')
        .Select(part => char.ToUpper(part[0]) + part.Substring(1)));
    
    // Handle reserved keywords and invalid identifiers
    if (!IsValidIdentifier(name))
        name = "@" + name;
    
    return name;
}
```

---

## Performance Considerations

### Build-Time Performance

- **Incremental Generation**: Only process changed `.md` files
- **Caching**: Store parsed models in generator execution context
- **Parallel Processing**: Markdig parsing is CPU-bound, can parallelize across files
- **Span<char> for parsing**: Reduce allocations in hot paths

**Target**: <100ms per file, <5s for 100 files on modern hardware

### Runtime Performance

- **No Runtime Overhead**: All work done at compile time
- **Generated BuildRenderTree**: Efficient rendering (same as hand-written Blazor)
- **Static HTML**: Rendered once, no re-parsing

---

## Security Considerations

### Compile-Time Security

- **No Code Execution**: Generator only emits source code, doesn't execute user content
- **Input Validation**: YAML and Markdown parsing uses well-tested libraries (YamlDotNet, Markdig)
- **Diagnostic on Errors**: All parsing errors result in build failures, not silent failures

### Generated Code Security

- **Standard Blazor Security**: Generated components inherit ComponentBase security model
- **No Dynamic Code Gen**: All code paths known at compile time
- **XSS Protection**: Blazor's default HTML encoding applies to all content

### Supply Chain

- **No New External Dependencies**: Uses existing project dependencies (Markdig, YamlDotNet)
- **In-Solution**: No external NuGet packages reduces supply chain attack surface

---

## Open Questions / Future Enhancements

**Resolved During Planning**:
- ✅ Source generator vs. runtime generation → Source generator (compile-time)
- ✅ Markdig extension approach → Custom inline parser
- ✅ Parameter declaration syntax → YAML front matter
- ✅ Output format → .md.g.cs files
- ✅ Integration method → In-solution project reference

**Potential Future Enhancements** (out of scope for initial implementation):
- Syntax highlighting for Markdown files with Razor in IDEs
- Custom Markdown extensions (e.g., callouts, admonitions)
- Support for scoped CSS (`.md.css` files)
- Live preview in IDE
- Migration tool from existing Razor components to Markdown

---

## References

- [Roslyn Source Generators Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Markdig GitHub Repository](https://github.com/xoofx/markdig)
- [YamlDotNet Documentation](https://github.com/aaubry/YamlDotNet/wiki)
- [Blazor ComponentBase API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase)
- [MDX (React) for inspiration](https://mdxjs.com/)
- [MD2RazorGenerator reference implementation](https://github.com/jsakamoto/MD2RazorGenerator)
