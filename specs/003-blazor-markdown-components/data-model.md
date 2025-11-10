# Data Model: Markdown to Razor Component Generator

**Feature**: 003-blazor-markdown-components  
**Date**: 2025-11-10

## Overview

This document defines the domain models used by the Markdown to Razor Component Generator. These models represent the parsed structure of Markdown files and their metadata, which are transformed into Blazor component source code.

---

## Core Entities

### MarkdownComponentModel

**Description**: Complete representation of a parsed Markdown file ready for code generation.

**Properties**:
- `FileName` (string): Original `.md` filename
- `ComponentName` (string): Generated C# class name (derived from filename)
- `Namespace` (string): Full namespace for generated class
- `Metadata` (ComponentMetadata): Parsed YAML front matter
- `Content` (MarkdownContent): Parsed Markdown body
- `CodeBlocks` (CodeBlock[]): Extracted `@code {}` blocks
- `SourceFilePath` (string): Absolute path to source `.md` file (for diagnostics)

**Relationships**:
- Has one `ComponentMetadata` (can be empty if no YAML front matter)
- Has one `MarkdownContent`
- Has zero or more `CodeBlock` instances

**Validation Rules**:
- `ComponentName` must be a valid C# identifier
- `Namespace` must be a valid C# namespace
- `FileName` must not be null or empty

**State Transitions**: N/A (immutable once parsed)

---

### ComponentMetadata

**Description**: Strongly-typed representation of YAML front matter configuration.

**Properties**:
- `Url` (string?): Single route URL (e.g., `/home`)
- `UrlArray` (string[]?): Multiple route URLs (e.g., `["/", "/home"]`)
- `Title` (string?): Page title for `<PageTitle>` component
- `Namespace` (string?): Override default namespace
- `Using` (string[]?): Using directives to add
- `Layout` (string?): Layout component name
- `Inherit` (string?): Base class override (default: ComponentBase)
- `Attribute` (string[]?): Attributes to apply to class
- `Parameters` (ParameterDefinition[]?): Component parameter declarations

**Relationships**:
- Belongs to one `MarkdownComponentModel`
- Has zero or more `ParameterDefinition` instances

**Validation Rules**:
- If both `Url` and `UrlArray` are specified → ERROR (mutually exclusive)
- All URLs must start with `/`
- `Namespace` must be valid C# namespace identifier if specified
- `Using` entries must be valid namespace identifiers
- `Layout` must be valid C# type identifier
- `Inherit` must be valid C# type identifier
- `Attribute` entries must be valid C# attribute syntax

**Example**:
```yaml
url: /blog
title: My Blog
$namespace: MyApp.Blog.Pages
$using: [MyApp.Services, System.Text.Json]
$layout: BlogLayout
$attribute: [Authorize(Roles = "Admin")]
$parameters:
  - Name: PostId
    Type: int
  - Name: Title
    Type: string
```

---

### ParameterDefinition

**Description**: Specification for a component parameter property.

**Properties**:
- `Name` (string): Parameter property name
- `Type` (string): C# type name (e.g., `string`, `int`, `List<User>`)

**Relationships**:
- Belongs to one `ComponentMetadata`

**Validation Rules**:
- `Name` must be valid C# property identifier
- `Name` must be PascalCase (convention check, warning if not)
- `Type` must be valid C# type syntax
- No duplicate `Name` values within same component

**Generated Code**:
```csharp
[Parameter]
public {Type} {Name} { get; set; }
```

---

### MarkdownContent

**Description**: Parsed and processed Markdown content ready for rendering.

**Properties**:
- `RawMarkdown` (string): Original Markdown text (before Markdig processing)
- `HtmlSegments` (HtmlSegment[]): Rendered HTML fragments with interleaved Razor syntax
- `ComponentReferences` (ComponentReference[]): Detected Blazor component usages

**Relationships**:
- Belongs to one `MarkdownComponentModel`
- Has zero or more `HtmlSegment` instances (ordered sequence)
- Has zero or more `ComponentReference` instances

**Processing Notes**:
- Markdig converts Markdown → HTML
- Custom Markdig pipeline preserves Razor syntax as `RazorSegment` instances
- Final output is sequence of HTML + Razor interleaved

---

### HtmlSegment

**Description**: Fragment of rendered content (either static HTML or dynamic Razor).

**Properties**:
- `Type` (SegmentType): `StaticHtml`, `RazorExpression`, `RazorCodeBlock`, `ComponentTag`
- `Content` (string): Raw content to emit
- `SequenceNumber` (int): Order in final output

**SegmentType Enum**:
- `StaticHtml`: Plain HTML from Markdown (e.g., `<h1>Title</h1>`)
- `RazorExpression`: Inline C# expression (e.g., `@DateTime.Now`, `@Model.Name`)
- `RazorCodeBlock`: Multi-line code block (e.g., `@code { int count = 0; }`)
- `ComponentTag`: Blazor component usage (e.g., `<Counter />`, `<Alert Severity="Warning">`)

**Rendering**:
- `StaticHtml` → `builder.AddMarkupContent(seq, content)`
- `RazorExpression` → `builder.AddContent(seq, {expression})`
- `RazorCodeBlock` → Extracted to class member, not in BuildRenderTree
- `ComponentTag` → `builder.OpenComponent<T>(seq); ... builder.CloseComponent()`

---

### ComponentReference

**Description**: Reference to another Blazor component used within Markdown.

**Properties**:
- `ComponentName` (string): Component type name (e.g., `Counter`, `Alert`)
- `Parameters` (ComponentParameter[]): Attributes passed to component
- `ChildContent` (string?): Content between opening and closing tags
- `SequenceNumber` (int): Position in render tree

**Relationships**:
- Belongs to one `MarkdownContent`
- Has zero or more `ComponentParameter` instances

**Example**:
```html
<Alert Severity="Warning" ShowIcon="true">This is a warning</Alert>
```
Parses to:
- `ComponentName`: "Alert"
- `Parameters`: [{ Name: "Severity", Value: "Warning" }, { Name: "ShowIcon", Value: "true" }]
- `ChildContent`: "This is a warning"

**Validation**:
- `ComponentName` must be valid C# type identifier
- Component type must be resolvable at compile time (Roslyn handles this)

---

### ComponentParameter

**Description**: Attribute passed to a component reference.

**Properties**:
- `Name` (string): Parameter name (e.g., `Severity`)
- `Value` (string): Parameter value expression (e.g., `"Warning"`, `@someVariable`)
- `IsExpression` (bool): True if value is C# expression (starts with @), false if string literal

**Generated Code**:
```csharp
// For <Alert Severity="Warning" Count="@itemCount">
builder.OpenComponent<Alert>(seq);
builder.AddAttribute(seq + 1, "Severity", "Warning");
builder.AddAttribute(seq + 2, "Count", itemCount); // Note: no @ in generated code
builder.CloseComponent();
```

---

### CodeBlock

**Description**: Extracted `@code {}` block to emit as class member.

**Properties**:
- `Content` (string): Raw C# code inside the block (without `@code {}` wrapper)
- `Location` (SourceLocation): Position in source file for diagnostics

**Processing**:
- Extracted before Markdown parsing
- Emitted after BuildRenderTree method in generated class
- Multiple code blocks concatenated

**Example**:
```markdown
@code {
    private int count = 0;
    
    void IncrementCount()
    {
        count++;
    }
}
```

Generates:
```csharp
public partial class MyComponent : ComponentBase
{
    protected override void BuildRenderTree(...) { }
    
    // Code block content emitted here:
    private int count = 0;
    
    void IncrementCount()
    {
        count++;
    }
}
```

---

## Supporting Types

### SourceLocation

**Description**: Position information for diagnostics.

**Properties**:
- `FilePath` (string): Absolute path to source file
- `LineNumber` (int): Line number (1-based)
- `ColumnNumber` (int): Column number (1-based)

**Usage**: Error reporting with precise location in source `.md` file

---

### SegmentType (Enum)

**Values**:
- `StaticHtml = 0`: Plain HTML content
- `RazorExpression = 1`: Inline C# expression (`@expr`)
- `RazorCodeBlock = 2`: Code block (`@code {}`)
- `ComponentTag = 3`: Blazor component reference (`<Component />`)

---

## Data Flow

1. **Input**: Markdown file (`.md`)
2. **Parse YAML Front Matter** → `ComponentMetadata`
3. **Extract Code Blocks** → `CodeBlock[]`
4. **Parse Markdown (Markdig + Custom Pipeline)**:
   - Markdown → HTML (Markdig)
   - Preserve Razor syntax (Custom Inline Parser)
   - Detect Component Tags → `ComponentReference[]`
   - Output → `HtmlSegment[]`
5. **Assemble** → `MarkdownComponentModel`
6. **Code Generation** → `.md.g.cs` file

---

## Validation Summary

| Entity | Validation Rules | Diagnostic Code |
|--------|------------------|-----------------|
| ComponentMetadata | Url and UrlArray are mutually exclusive | MD001 |
| ComponentMetadata | All URLs start with `/` | MD002 |
| ParameterDefinition | Name is valid C# identifier | MD003 |
| ParameterDefinition | Type is valid C# type syntax | MD004 |
| ParameterDefinition | No duplicate names in same component | MD005 |
| ComponentReference | ComponentName is valid type identifier | MD006 |
| CodeBlock | Content is valid C# syntax | Roslyn handles |

**Note**: Many validations (C# syntax correctness) are delegated to Roslyn compiler after code generation, providing precise diagnostic messages.

---

## Example Complete Model

**Input** (`Blog.md`):
```markdown
---
url: /blog
title: My Blog
$parameters:
  - Name: PostId
    Type: int
---

# Blog Post @PostId

<Alert Severity="Info">Welcome</Alert>

@code {
    private string author = "John";
}
```

**Parsed Model**:
```csharp
new MarkdownComponentModel
{
    FileName = "Blog.md",
    ComponentName = "Blog",
    Namespace = "MyApp.Pages",
    SourceFilePath = "/path/to/Blog.md",
    
    Metadata = new ComponentMetadata
    {
        Url = "/blog",
        Title = "My Blog",
        Parameters = new[]
        {
            new ParameterDefinition { Name = "PostId", Type = "int" }
        }
    },
    
    Content = new MarkdownContent
    {
        RawMarkdown = "# Blog Post @PostId\n\n<Alert>...</Alert>",
        HtmlSegments = new[]
        {
            new HtmlSegment
            {
                Type = SegmentType.StaticHtml,
                Content = "<h1>Blog Post ",
                SequenceNumber = 0
            },
            new HtmlSegment
            {
                Type = SegmentType.RazorExpression,
                Content = "@PostId",
                SequenceNumber = 1
            },
            new HtmlSegment
            {
                Type = SegmentType.StaticHtml,
                Content = "</h1>",
                SequenceNumber = 2
            },
            new HtmlSegment
            {
                Type = SegmentType.ComponentTag,
                Content = "<Alert Severity=\"Info\">Welcome</Alert>",
                SequenceNumber = 3
            }
        },
        ComponentReferences = new[]
        {
            new ComponentReference
            {
                ComponentName = "Alert",
                Parameters = new[]
                {
                    new ComponentParameter
                    {
                        Name = "Severity",
                        Value = "Info",
                        IsExpression = false
                    }
                },
                ChildContent = "Welcome",
                SequenceNumber = 3
            }
        }
    },
    
    CodeBlocks = new[]
    {
        new CodeBlock
        {
            Content = "private string author = \"John\";",
            Location = new SourceLocation
            {
                FilePath = "/path/to/Blog.md",
                LineNumber = 12,
                ColumnNumber = 1
            }
        }
    }
}
```

---

## Design Notes

**Immutability**: All model classes are immutable once created (init-only properties or constructor assignment). This ensures thread-safety during incremental generation.

**Performance**: Models use arrays instead of lists where possible to reduce allocations. Strings are pooled where repeated (e.g., namespace, common types).

**Extensibility**: Model structure allows future additions (e.g., scoped CSS, custom Markdown extensions) without breaking changes.
