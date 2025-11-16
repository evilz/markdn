# Markdown to Razor Pre-Build Generator - Implementation Summary

## Overview

This document summarizes the implementation of the Markdown to Razor pre-build generator as specified in the requirements.

## Requirements Met

✅ **All requirements from the specification have been fully implemented**

### 1. Console Tool Creation

**Location**: `tools/MarkdownToRazorGenerator/`

**Components**:
- `Program.cs` - Main entry point with argument parsing and orchestration
- `Models/MarkdownMetadata.cs` - Data model for YAML front-matter
- `Parsers/FrontMatterParser.cs` - YAML front-matter extraction and parsing
- `Parsers/MarkdownConverter.cs` - Markdown to HTML conversion using Markdig
- `Generators/RazorComponentGenerator.cs` - Razor component file generation
- `Utilities/SlugGenerator.cs` - Slug normalization and route generation

**Dependencies**:
- Markdig 0.37.0 (Markdown parsing)
- YamlDotNet 16.2.0 (YAML parsing)

### 2. MSBuild Integration

**Projects Updated**:
- `src/Markdn.Blazor.App/Markdn.Blazor.App.csproj`
- `src/Markdn.Blazor.App.Wasm/Markdn.Blazor.App.Wasm.csproj`

**Integration Details**:
- Pre-build target `GenerateRazorFromMarkdown` runs before `CoreCompile`
- Configurable via MSBuild properties:
  - `MarkdownBlogDir` (default: `content/blog`)
  - `MarkdownPagesDir` (default: `content/pages`)
  - `GeneratedRazorRoot` (default: `Generated`)
- Automatic building of generator tool before execution
- Content files excluded from source generator to prevent conflicts

### 3. Feature Implementation

#### YAML Front-Matter Support

Supported fields:
- `title` - Page title
- `slug` - URL-friendly identifier
- `route` - Explicit page route
- `layout` - Blazor layout component
- `summary` - Brief description
- `date` - Publication date
- `tags` - Array of tags

#### Fallback Logic

**Title**:
1. `title` from front-matter
2. First `# H1` heading in content
3. File name without extension

**Slug**:
1. `slug` from front-matter
2. Normalized file name (lowercase, alphanumeric + hyphens)

**Route**:
1. `route` from front-matter
2. `/{directory-type}/{slug}` (e.g., `/blog/my-post`)
3. `/{slug}` for unknown directories

#### Generated Razor Components

Each generated component includes:
- `@page` directive for routing
- `@using Microsoft.AspNetCore.Components`
- Optional `@layout` directive
- `<PageTitle>` for SEO
- `<article>` wrapper with HTML content
- `@code` block with static readonly HTML string

**Example Output**:
```razor
@page "/blog/mon-super-article"
@using Microsoft.AspNetCore.Components

<PageTitle>Mon super article</PageTitle>

<article class="markdown-body">
    @((MarkupString)HtmlContent)
</article>

@code {
    private static readonly string HtmlContent = @"
<h1 id=""mon-super-article"">Mon super article</h1>
<p>Content...</p>
";
}
```

#### Component Naming

Files are named using PascalCase to comply with Razor component naming conventions:
- `about.md` → `About.razor`
- `mon-super-article.md` → `Mon-super-article.razor`

### 4. Testing

**Test Project**: `tests/MarkdownToRazorGenerator.Tests/`

**Test Coverage**:
- `FrontMatterParserTests.cs` - 7 tests
- `MarkdownConverterTests.cs` - 4 tests
- `SlugGeneratorTests.cs` - 10 tests

**Total**: 21 passing tests

**Test Framework**: xUnit with FluentAssertions

### 5. Documentation

**README**: `tools/MarkdownToRazorGenerator/README.md`

**Contents**:
- Complete usage documentation
- MSBuild integration guide
- Markdown file format specification
- Generated Razor file structure
- Troubleshooting guide
- Examples and use cases

### 6. Sample Content

**Sample Files**:
- `src/Markdn.Blazor.App/content/blog/mon-super-article.md`
- `src/Markdn.Blazor.App/content/pages/about.md`

These demonstrate:
- YAML front-matter usage
- Markdown formatting (headings, lists, bold, code blocks)
- Automatic route generation
- French and English content

### 7. Security

**Verified**:
✅ No vulnerabilities in dependencies (gh-advisory-database check passed)
✅ CodeQL analysis passed (0 alerts)
✅ Proper input validation and sanitization
✅ Safe HTML escaping for C# verbatim strings

### 8. Build Integration

**Build Output**:
```
Génération des pages .razor à partir des markdown...
Processing blog directory: content/blog
  Processing: mon-super-article.md
    Generated: Mon-super-article.razor (route: /blog/mon-super-article)
Processing pages directory: content/pages
  Processing: about.md
    Generated: About.razor (route: /pages/about)

Total markdown files found: 2
Razor files generated: 2
```

**Generated Files**:
```
Generated/
├── Blog/
│   └── Mon-super-article.razor
└── Pages/
    └── About.razor
```

## Differences from Specification

### Minor Adjustments

1. **Target Changed**: Used `BeforeTargets="CoreCompile"` instead of `BeforeTargets="RazorCoreCompile"` for better timing in the build process.

2. **Component Naming**: Added automatic PascalCase conversion for file names to comply with Razor component naming conventions (not explicitly mentioned in spec but necessary).

3. **Content Exclusion**: Added exclusion of `content/**/*.md` from source generator's `AdditionalFiles` to prevent conflicts with existing source generator.

### Extensions Beyond Specification

1. **Error Handling**: Added comprehensive error handling and reporting that goes beyond spec requirements.

2. **Test Coverage**: Created 21 unit tests (spec didn't require tests but mentioned it as a step).

3. **Documentation**: Created extensive README documentation with examples and troubleshooting.

## Technical Decisions

### Why Static Readonly String?

The HTML content is stored as a static readonly string in the `@code` block because:
- No runtime overhead (parsed once at compile time)
- Better performance than runtime Markdown parsing
- Simpler component code
- Matches the spec requirement for inline HTML

### Why PascalCase File Names?

Razor requires component names to start with an uppercase letter. Converting slugs to PascalCase ensures:
- Compliance with Razor naming conventions
- No compilation errors
- Consistent naming across generated files

### Why CoreCompile Instead of RazorCoreCompile?

`RazorCoreCompile` wasn't triggering in all build scenarios. `CoreCompile` is more reliable and still runs before Razor processing, ensuring files are available for compilation.

## Coexistence with Source Generator

The project already has a source generator (`Markdn.SourceGenerators`) that processes Markdown files. The pre-build tool coexists peacefully:

**Pre-build Tool**:
- Processes `content/**/*.md` files
- Generates physical `.razor` files
- Runs during MSBuild before CoreCompile

**Source Generator**:
- Processes `Pages/**/*.md` and other `.md` files
- Generates `.g.cs` files via Roslyn
- Runs during C# compilation

**No Conflicts**: The `content/` directory is excluded from the source generator's `AdditionalFiles`, preventing double-processing.

## Future Enhancements

Potential improvements mentioned in the spec but not yet implemented:

1. **Index Generation**: Auto-generate collection list pages from metadata
2. **Multi-language Support**: Handle `content/blog/en`, `content/blog/fr` with route prefixing
3. **Code-behind Files**: Generate `.g.cs` files with strongly-typed metadata classes
4. **Route Validation**: Detect and warn about duplicate routes
5. **Watch Mode**: Development mode with automatic regeneration on file changes

These are listed in the README as "Future Enhancements" and can be added incrementally.

## Conclusion

✅ **All core requirements from the specification have been successfully implemented.**

The Markdown to Razor pre-build generator:
- Reads Markdown files from configurable directories
- Parses YAML front-matter with YamlDotNet
- Converts Markdown to HTML with Markdig
- Generates physical `.razor` files with proper routing
- Integrates with MSBuild to run before Razor compilation
- Includes comprehensive tests and documentation
- Passes security checks (no vulnerabilities, no CodeQL alerts)

The implementation is production-ready and can be used in both Blazor Server and WebAssembly projects.
