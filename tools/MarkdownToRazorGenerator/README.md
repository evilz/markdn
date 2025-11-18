# Markdown to Razor Pre-Build Generator

A console tool that generates Blazor Razor components from Markdown files during the build process, before Razor compilation.

## Overview

This tool provides an alternative approach to the source generator for creating Blazor components from Markdown content. It processes Markdown files in designated directories (e.g., `content/blog`, `content/pages`) and generates physical `.razor` files that become routable Blazor pages.

## Features

- 📝 **YAML Front-Matter Parsing**: Uses Markdig's YamlFrontMatter extension with simplified extension methods
- 🔄 **Markdig Conversion**: Converts Markdown to HTML with advanced extensions (tables, task lists, etc.)
- 🎯 **Automatic Routing**: Generates `@page` directives based on file structure or metadata
- 📄 **Page Title Generation**: Creates `<PageTitle>` from front-matter or file content
- 🎨 **Layout Support**: Respects custom layout specifications from front-matter
- 🔧 **MSBuild Integration**: Runs automatically before Razor compilation
- ⚙️ **Configurable Paths**: Customize input and output directories via MSBuild properties

## Architecture

The tool is structured as follows:

```
tools/MarkdownToRazorGenerator/
├── Extensions/
│   └── MarkdownExtensions.cs      # Markdown front-matter extension methods
├── Models/
│   └── MarkdownMetadata.cs       # Front-matter data model
├── Parsers/
│   ├── FrontMatterParser.cs      # YAML front-matter extraction
│   └── MarkdownConverter.cs      # Markdig HTML conversion
├── Generators/
│   └── RazorComponentGenerator.cs # Razor file generation
├── Utilities/
│   └── SlugGenerator.cs          # Slug and route normalization
└── Program.cs                     # Main entry point and orchestration
```

## Usage

### Command Line

```bash
dotnet run --project tools/MarkdownToRazorGenerator/MarkdownToRazorGenerator.csproj \
  <project-directory> \
  [--blogDir <path>] \
  [--pagesDir <path>] \
  [--outputRoot <path>]
```

**Arguments:**
- `<project-directory>`: Path to the Blazor project root (required)
- `--blogDir`: Path to blog Markdown files (default: `content/blog`)
- `--pagesDir`: Path to pages Markdown files (default: `content/pages`)
- `--outputRoot`: Root directory for generated files (default: `Generated`)

### MSBuild Integration

The tool is integrated into Blazor projects via a pre-build target. It runs automatically before `CoreCompile`.

#### Configuration Properties

Add these properties to your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Markdown to Razor Generator Configuration -->
  <MarkdownBlogDir>content/blog</MarkdownBlogDir>
  <MarkdownPagesDir>content/pages</MarkdownPagesDir>
  <GeneratedRazorRoot>Generated</GeneratedRazorRoot>
</PropertyGroup>
```

#### Pre-Build Target

The target is automatically included in projects configured for this feature:

```xml
<Target Name="GenerateRazorFromMarkdown" BeforeTargets="CoreCompile">
  <Message Text="Génération des pages .razor à partir des markdown..." Importance="high" />
  
  <!-- Build the generator tool first -->
  <Exec Command="dotnet build &quot;...&quot;" />
  
  <!-- Run the generator tool -->
  <Exec Command="dotnet run --no-build --project &quot;...&quot; &quot;$(MSBuildProjectDirectory)&quot; ..." />
</Target>
```

## Markdown File Format

### Using Extension Methods

The tool provides convenient extension methods for working with markdown content and front matter:

```csharp
using MarkdownToRazorGenerator.Extensions;

var markdown = @"---
title: My Post
slug: my-post
---

# Content Here";

// Extract typed front matter
var metadata = markdown.GetFrontMatter<MarkdownMetadata>();
Console.WriteLine(metadata?.Title); // "My Post"

// Get raw YAML front matter
var yaml = markdown.GetFrontMatterYaml();

// Get markdown body (without front matter)
var body = markdown.GetMarkdownBody(); // "# Content Here"
```

These extension methods use Markdig's built-in `YamlFrontMatterBlock` parser for reliable and efficient parsing.

### Front-Matter (YAML)

Optional YAML front-matter at the beginning of Markdown files:

```markdown
---
title: "My Blog Post"
slug: "my-blog-post"
route: "/blog/my-blog-post"
layout: "BlogLayout"
summary: "A brief description"
date: "2025-11-15"
tags:
  - blazor
  - markdown
---

# Content starts here

Your Markdown content...
```

### Front-Matter Fields

| Field | Type | Description |
|-------|------|-------------|
| `title` | string | Page title (used in `<PageTitle>`) |
| `slug` | string | URL-friendly identifier |
| `route` | string | Explicit route for the page |
| `layout` | string | Blazor layout component name |
| `summary` | string | Brief description (not currently used in output) |
| `date` | datetime | Publication date |
| `tags` | array | List of tags |

### Fallback Logic

The generator implements intelligent fallbacks when front-matter is missing:

#### Title
1. `title` from front-matter
2. First `# H1` heading in Markdown content
3. File name without extension

#### Slug
1. `slug` from front-matter
2. Normalized file name (lowercase, hyphens, alphanumeric only)

#### Route
1. `route` from front-matter
2. `/{directory-type}/{slug}` (e.g., `/blog/my-post`)
3. `/{slug}` if directory type is unknown

## Generated Razor Files

The tool generates Razor components with the following structure:

```razor
@page "/blog/my-post"
@using Microsoft.AspNetCore.Components

@layout BlogLayout

<PageTitle>My Blog Post</PageTitle>

<article class="markdown-body">
    @((MarkupString)HtmlContent)
</article>

@code {
    private static readonly string HtmlContent = @"
<h1 id=""my-post"">My Blog Post</h1>
<p>Content here...</p>
";
}
```

**Features:**
- `@page` directive for routing
- Optional `@layout` directive
- `<PageTitle>` for SEO
- HTML content in a static readonly string (no runtime overhead)
- Proper escaping for C# verbatim strings

## Output Structure

Generated files are organized by directory type:

```
Generated/
├── Blog/
│   ├── first-post.razor
│   └── second-post.razor
└── Pages/
    ├── about.razor
    └── contact.razor
```

## Integration with Existing Projects

### Blazor Server / SSR

The generated `.razor` files work seamlessly with Blazor Server and SSR projects. No additional configuration is needed beyond the MSBuild integration.

### Blazor WebAssembly

Works identically to Server projects. The generated components are compiled into the WASM bundle.

### Coexistence with Source Generator

This pre-build tool can coexist with the existing source generator:

- **Pre-build tool**: Processes `content/**/*.md` files → generates physical `.razor` files
- **Source generator**: Processes other `.md` files (e.g., `Pages/*.md`) → generates `.g.cs` code files

To prevent conflicts, exclude the `content/` directory from the source generator:

```xml
<ItemGroup>
  <!-- Exclude content directory from source generator -->
  <AdditionalFiles Include="**\*.md" Exclude="wwwroot\**\*.md;content\**\*.md" />
</ItemGroup>
```

## Build Artifacts

Generated `.razor` files are build artifacts and should not be committed to source control. The `.gitignore` includes:

```gitignore
# Generated Razor files from Markdown (build artifacts)
**/Generated/**/*.razor
```

## Error Handling

The tool provides comprehensive error handling:

- **YAML Parsing Errors**: Logged with file name and error details
- **File System Errors**: Gracefully handled with informative messages
- **Build Integration**: Non-zero exit code on errors (fails the build)

### Example Error Output

```
Processing: invalid-yaml.md
  Warning: YAML parsing error: (Line: 2, Col: 1) - Invalid YAML syntax
  Generated: invalid-yaml.razor (route: /blog/invalid-yaml)

Total markdown files found: 5
Razor files generated: 5
Errors encountered: 1
  - invalid-yaml.md: YAML parsing error: ...
```

## Performance

- **Incremental Builds**: Only regenerates files when source Markdown changes (MSBuild handles this automatically)
- **Fast Execution**: Typical runtime < 1 second for 100 files
- **No Runtime Overhead**: Generated code uses static strings (no parsing at runtime)

## Examples

### Example 1: Blog Post

**Input** (`content/blog/getting-started.md`):
```markdown
---
title: "Getting Started with Blazor"
date: "2025-11-15"
tags: [blazor, tutorial]
---

# Getting Started

Welcome to Blazor!
```

**Output** (`Generated/Blog/getting-started.razor`):
```razor
@page "/blog/getting-started"
@using Microsoft.AspNetCore.Components

<PageTitle>Getting Started with Blazor</PageTitle>

<article class="markdown-body">
    @((MarkupString)HtmlContent)
</article>

@code {
    private static readonly string HtmlContent = @"
<h1 id=""getting-started"">Getting Started</h1>
<p>Welcome to Blazor!</p>
";
}
```

### Example 2: Custom Route and Layout

**Input** (`content/pages/special.md`):
```markdown
---
title: "Special Page"
route: "/custom/special"
layout: "CustomLayout"
---

# Special Content
```

**Output** (`Generated/Pages/special.razor`):
```razor
@page "/custom/special"
@using Microsoft.AspNetCore.Components

@layout CustomLayout

<PageTitle>Special Page</PageTitle>

<article class="markdown-body">
    @((MarkupString)HtmlContent)
</article>

@code {
    private static readonly string HtmlContent = @"
<h1 id=""special-content"">Special Content</h1>
";
}
```

## Troubleshooting

### Generator Doesn't Run

1. **Check MSBuild target**: Ensure `GenerateRazorFromMarkdown` target exists in `.csproj`
2. **Clean build**: Run `dotnet clean` then `dotnet build`
3. **Verify paths**: Check that `MarkdownBlogDir` and `MarkdownPagesDir` point to existing directories

### Generated Files Not Compiled

1. **Check Content includes**: Ensure `<Content Include="$(GeneratedRazorRoot)\**\*.razor" />` is in `.csproj`
2. **Rebuild**: Generated files should be included in subsequent build steps

### Conflicts with Source Generator

1. **Exclude overlapping files**: Update `<AdditionalFiles>` to exclude `content/**/*.md`
2. **Use different directories**: Keep pre-build content separate from source generator content

## Future Enhancements

Potential improvements (not currently implemented):

- Support for multilingual content (`content/blog/en`, `content/blog/fr`)
- Generation of index pages (collection lists)
- Code-behind `.g.cs` files with strongly-typed metadata
- Validation of duplicate routes
- Watch mode for development (auto-regenerate on file changes)

## License

Part of the Markdn project. See main repository LICENSE file.
