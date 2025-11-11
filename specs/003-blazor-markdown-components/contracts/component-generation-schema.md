
# Component Generation Schema & Diagnostics

This document lists the generator diagnostics (MD00x) emitted by the Markdown → Razor component generator and their meaning. Use these codes in CI and troubleshooting.

| Code | Severity | Meaning | Recommended action |
|------|----------|---------|-------------------|
| MD001 | Error | Invalid YAML front matter parsing error | Fix YAML syntax in the Markdown front matter (missing `:` or incorrect indentation) |
| MD002 | Error | Invalid URL format in `url` or `url` array (must start with `/`) | Ensure route URLs begin with `/` |
| MD003 | Error | Invalid parameter name in `$parameters` (not a valid C# identifier) | Rename parameter to a valid C# identifier (PascalCase recommended) |
| MD004 | Error | Invalid parameter type syntax in `$parameters` | Correct the type syntax (e.g., `string`, `int`, `List<string>`) |
| MD005 | Error | Duplicate parameter name declared in `$parameters` | Remove duplicate entries or rename parameters |
| MD006 | Warning | Component reference may not be resolvable at generation time | Add `$using` or `componentNamespaces` to front matter or ensure component type is available to the compilation unit |
| MD007 | Error | Malformed Razor syntax detected while preserving Razor fragments (unmatched braces/parentheses) | Fix Razor syntax in the Markdown (e.g., close `@code {}` or `@if (...) { }` blocks) |
| MD008 | Error | Mutually exclusive URL properties: both `url` (scalar) and `url` (array) provided | Use one form only; prefer `url:` (string) or `url:` (array) but not both |

Examples

Malformed Razor (MD007):

```markdown
@code {
  private int x = 0;
  // missing closing brace -> generator will emit MD007

```

Unresolvable component (MD006):

```markdown
<UnknownComponent />

```

Remediation
- Add explicit `$using` or `componentNamespaces` in front matter when referencing components from other namespaces.
- Fix YAML syntax in front matter.
- Correct Razor blocks and expressions.
# Component Generation Contract

**Feature**: 003-blazor-markdown-components  
**Date**: 2025-11-10

## Overview

This document defines the contract between Markdown input files and generated Blazor component source code. It specifies the input format (YAML front matter + Markdown + Razor syntax) and the guaranteed output structure.

---

## Input Contract: Markdown File Format

### File Extension
- **Required**: `.md`
- **Location**: Anywhere in Blazor project
- **Build Action**: Automatically included as `AdditionalFiles` by generator

### File Structure
```markdown
---
[YAML Front Matter - Optional]
---

[Markdown Content with optional Razor syntax]
```

---

## YAML Front Matter Schema

### Supported Keys

| Key | Type | Required | Default | Description |
|-----|------|----------|---------|-------------|
| `url` | string OR array | No | null | Route URL(s) for component. Generates `@page` directive. |
| `title` | string | No | null | Page title. Generates `<PageTitle>` component. |
| `$namespace` | string | No | Auto-generated | Override default namespace. |
| `$using` | array | No | [] | Using directives to add. |
| `$layout` | string | No | null | Layout component. Generates `@layout` directive. |
| `$inherit` | string | No | "ComponentBase" | Base class. Generates `@inherits` directive. |
| `$attribute` | array | No | [] | Class attributes. Generates `@attribute` directives. |
| `$parameters` | array | No | [] | Component parameters. See Parameter Schema below. |

### Parameter Schema

Each entry in `$parameters` must have:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Name` | string | Yes | Parameter property name (PascalCase recommended) |
| `Type` | string | Yes | C# type (e.g., `string`, `int`, `List<User>`) |

### Example: Complete YAML Front Matter

```yaml
---
url: [/, /home]
title: Home Page
$namespace: MyApp.Pages
$using:
  - MyApp.Services
  - System.Text.Json
$layout: MainLayout
$attribute:
  - Authorize(Roles = "Admin")
  - RequireHttps
$parameters:
  - Name: UserId
    Type: int
  - Name: DisplayMode
    Type: string
---
```

### Validation Rules

1. **Mutually Exclusive**: Cannot specify both `url` (string) and `url` (array) - use one or the other
2. **URL Format**: All URLs must start with `/`
3. **Identifier Validation**: `$namespace`, `$layout`, `$inherit` must be valid C# identifiers
4. **Parameter Names**: Must be valid C# property names
5. **Parameter Types**: Must be valid C# type syntax
6. **No Duplicates**: Parameter names must be unique within same component

---

## Razor Syntax Support in Markdown

### Inline Expressions

**Syntax**: `@<expression>`

**Examples**:
```markdown
Current time: @DateTime.Now
User: @UserName
Count: @(items.Count * 2)
```

**Generated Code**:
```csharp
builder.AddContent(seq, DateTime.Now);
builder.AddContent(seq, UserName);
builder.AddContent(seq, (items.Count * 2));
```

### Code Blocks

**Syntax**: `@code { <C# code> }`

**Example**:
```markdown
@code {
    private int count = 0;
    private string message = "Hello";
    
    protected override void OnInitialized()
    {
        message = $"Hello at {DateTime.Now}";
    }
    
    private void IncrementCount()
    {
        count++;
    }
}
```

**Generated Code**: Emitted as class members after `BuildRenderTree` method

**Multiple Code Blocks**: Concatenated in order of appearance

### Component References

**Syntax**: `<ComponentName [Parameters] />`  
**Syntax with Content**: `<ComponentName [Parameters]>Child Content</ComponentName>`

**Examples**:
```markdown
<Counter />
<Alert Severity="Warning">This is a warning</Alert>
<Button OnClick="@HandleClick" Disabled="@isDisabled">Click Me</Button>
```

**Parameter Types**:
- String literal: `Severity="Warning"`
- Expression: `OnClick="@HandleClick"`
- Boolean: `Disabled="@isDisabled"`

**Generated Code**:
```csharp
builder.OpenComponent<Counter>(seq);
builder.CloseComponent();

builder.OpenComponent<Alert>(seq);
builder.AddAttribute(seq + 1, "Severity", "Warning");
builder.AddAttribute(seq + 2, "ChildContent", (RenderFragment)(builder2 => {
    builder2.AddMarkupContent(seq, "This is a warning");
}));
builder.CloseComponent();
```

---

## Output Contract: Generated C# Source

### File Naming Convention

**Pattern**: `<OriginalFileName>.md.g.cs`

**Examples**:
- `Home.md` → `Home.md.g.cs`
- `Blog/Post.md` → `Post.md.g.cs` (in Blog subfolder namespace)

### Class Naming Convention

**Pattern**: PascalCase filename without extension

**Rules**:
1. Remove `.md` extension
2. Remove date prefix (YYYY-MM-DD-) if present
3. Convert kebab-case to PascalCase
4. Prefix `@` if reserved keyword

**Examples**:
- `home.md` → `Home` class
- `my-blog-post.md` → `MyBlogPost` class
- `2024-11-10-article.md` → `Article` class
- `class.md` → `@class` class

### Generated Class Structure

```csharp
// <auto-generated />
// This file is auto-generated by Markdn.SourceGenerators.
// Do not edit this file directly. Edit the source .md file instead.

#nullable enable

namespace <Namespace>
{
    [Route("<url>")] // If url specified
    [<Attributes>]   // If $attribute specified
    public partial class <ComponentName> : <InheritClass>
    {
        <ParameterProperties> // If $parameters specified
        
        protected override void BuildRenderTree(
            Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            <RenderTreeStatements>
        }
        
        <CodeBlockContent> // If @code blocks present
    }
}
```

### Namespace Generation

**Default Rule**: 
```
<ProjectRootNamespace>.<RelativeFolderPath>
```

**Examples**:
- `MyApp.csproj` + `Pages/Home.md` → `namespace MyApp.Pages`
- `MyApp.csproj` + `Components/Shared/Alert.md` → `namespace MyApp.Components.Shared`

**Override**: Respect `$namespace` in YAML if specified

### Parameter Property Generation

**Input**:
```yaml
$parameters:
  - Name: Title
    Type: string
  - Name: Count
    Type: int
```

**Output**:
```csharp
[Microsoft.AspNetCore.Components.ParameterAttribute]
public string Title { get; set; } = default!;

[Microsoft.AspNetCore.Components.ParameterAttribute]
public int Count { get; set; }
```

**Nullable Handling**: Value types not nullable, reference types nullable (`= default!`)

---

## BuildRenderTree Generation Patterns

### Static HTML (from Markdown)

**Input**: `# Hello, World!`

**Output**:
```csharp
builder.AddMarkupContent(0, "<h1>Hello, World!</h1>");
```

### Inline Razor Expression

**Input**: `Current user: @UserName`

**Output**:
```csharp
builder.AddMarkupContent(0, "Current user: ");
builder.AddContent(1, UserName);
```

### Component Reference (No Parameters)

**Input**: `<Counter />`

**Output**:
```csharp
builder.OpenComponent<Counter>(0);
builder.CloseComponent();
```

### Component Reference (With Parameters)

**Input**: `<Alert Severity="Warning" Count="@itemCount">Message</Alert>`

**Output**:
```csharp
builder.OpenComponent<Alert>(0);
builder.AddAttribute(1, "Severity", "Warning");
builder.AddAttribute(2, "Count", itemCount);
builder.AddAttribute(3, "ChildContent", (RenderFragment)(builder2 => {
    builder2.AddMarkupContent(0, "Message");
}));
builder.CloseComponent();
```

### Mixed Content

**Input**:
```markdown
# Title

Some text with @expression.

<Component />

More markdown content.
```

**Output**:
```csharp
builder.AddMarkupContent(0, "<h1>Title</h1>");
builder.AddMarkupContent(1, "<p>Some text with ");
builder.AddContent(2, expression);
builder.AddMarkupContent(3, ".</p>");
builder.OpenComponent<Component>(4);
builder.CloseComponent();
builder.AddMarkupContent(5, "<p>More markdown content.</p>");
```

---

## Directive Generation

### @page Directive

**Input (YAML)**:
```yaml
url: /home
```

**Output**:
```csharp
[Microsoft.AspNetCore.Components.RouteAttribute("/home")]
```

**Multiple Routes**:
```yaml
url: [/, /home]
```

**Output**:
```csharp
[Microsoft.AspNetCore.Components.RouteAttribute("/")]
[Microsoft.AspNetCore.Components.RouteAttribute("/home")]
```

### @layout Directive

**Input**:
```yaml
$layout: MainLayout
```

**Output**:
```csharp
[Microsoft.AspNetCore.Components.LayoutAttribute(typeof(MainLayout))]
```

### @inherits Directive

**Input**:
```yaml
$inherit: CustomComponentBase
```

**Output**:
```csharp
public partial class MyComponent : CustomComponentBase
```

### @using Directives

**Input**:
```yaml
$using:
  - MyApp.Services
  - System.Text.Json
```

**Output** (at top of file):
```csharp
using MyApp.Services;
using System.Text.Json;
```

### @attribute Directive

**Input**:
```yaml
$attribute:
  - Authorize(Roles = "Admin")
  - RequireHttps
```

**Output**:
```csharp
[Authorize(Roles = "Admin")]
[RequireHttps]
public partial class MyComponent : ComponentBase
```

---

## Error Handling Contract

### Diagnostic Codes

| Code | Severity | Description |
|------|----------|-------------|
| MD001 | Error | Invalid YAML front matter syntax |
| MD002 | Error | URL must start with `/` |
| MD003 | Error | Parameter name is not valid C# identifier |
| MD004 | Error | Parameter type is not valid C# type syntax |
| MD005 | Error | Duplicate parameter name |
| MD006 | Warning | Component reference may not be resolvable |
| MD007 | Error | Malformed Razor syntax |
| MD008 | Error | Multiple url and urlArray both specified |

### Diagnostic Output Format

```
MD001: Invalid YAML front matter in Home.md: unexpected token on line 3
  at Home.md:3:5
```

**Integration**: Diagnostics appear in:
- Build output
- IDE Error List
- CI/CD build logs

---

## Versioning & Compatibility

**Generator Version**: Embedded in generated file comment
```csharp
// <auto-generated by Markdn.SourceGenerators v1.0.0 />
```

**Breaking Changes**: Any change to YAML schema or output format increments major version

**Backward Compatibility**: Older `.md` files work with newer generator (additive changes only)

---

## Complete Example

### Input: `Blog.md`

```markdown
---
url: /blog
title: My Blog
$namespace: MyApp.Pages.Blog
$parameters:
  - Name: PostId
    Type: int
---

# Blog Post @PostId

Posted by @Author on @PostDate.ToShortDateString()

<Alert Severity="Info">This is post number @PostId</Alert>

## Comments

<CommentList PostId="@PostId" />

@code {
    [Parameter]
    public string Author { get; set; } = "Anonymous";
    
    [Parameter]
    public DateTime PostDate { get; set; } = DateTime.Now;
}
```

### Output: `Blog.md.g.cs`

```csharp
// <auto-generated by Markdn.SourceGenerators v1.0.0 />
// This file is auto-generated. Do not edit directly.

#nullable enable

namespace MyApp.Pages.Blog
{
    [Microsoft.AspNetCore.Components.RouteAttribute("/blog")]
    public partial class Blog : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public int PostId { get; set; }
        
        protected override void BuildRenderTree(
            Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.AddMarkupContent(0, "<h1>Blog Post ");
            builder.AddContent(1, PostId);
            builder.AddMarkupContent(2, "</h1>");
            
            builder.AddMarkupContent(3, "<p>Posted by ");
            builder.AddContent(4, Author);
            builder.AddMarkupContent(5, " on ");
            builder.AddContent(6, PostDate.ToShortDateString());
            builder.AddMarkupContent(7, ".</p>");
            
            builder.OpenComponent<Alert>(8);
            builder.AddAttribute(9, "Severity", "Info");
            builder.AddAttribute(10, "ChildContent", (RenderFragment)(builder2 => {
                builder2.AddMarkupContent(0, "This is post number ");
                builder2.AddContent(1, PostId);
            }));
            builder.CloseComponent();
            
            builder.AddMarkupContent(11, "<h2>Comments</h2>");
            
            builder.OpenComponent<CommentList>(12);
            builder.AddAttribute(13, "PostId", PostId);
            builder.CloseComponent();
        }
        
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Author { get; set; } = "Anonymous";
        
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public DateTime PostDate { get; set; } = DateTime.Now;
    }
}
```

---

## Implementation Notes

**Sequence Numbers**: Must be unique and sequential for RenderTreeBuilder (Blazor requirement)

**Null Safety**: Generated code uses `#nullable enable` and follows nullable reference type conventions

**Partial Classes**: Generated as `partial` to allow manual extension in separate files if needed

**Performance**: Static HTML content is emitted as single `AddMarkupContent` call (efficient)

**Hot Reload**: Changes to `.md` file trigger regeneration; Blazor hot reload picks up the change automatically
