# Quickstart: Blazor Markdown Components

**Get started with Markdown-based Blazor components in 5 minutes**

---

## What You'll Build

A Blazor component written in Markdown that renders as a routable page with parameters, inline expressions, and child components.

---

## Prerequisites

- .NET 8.0 SDK or later
- A Blazor project (Server, WebAssembly, or Auto)
- Basic familiarity with Blazor components and Markdown

---

## Step 1: Add the Source Generator

### Option A: In-Solution Reference (Recommended for Development)

1. Clone the Markdn repository or add the `Markdn.SourceGenerators` project to your solution
2. Reference the generator in your Blazor project's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Markdn.SourceGenerators\Markdn.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

> **Note**: `OutputItemType="Analyzer"` treats the generator as a compile-time analyzer, not a runtime dependency.

### Option B: NuGet Package (Future)

```bash
dotnet add package Markdn.SourceGenerators
```

---

## Step 2: Create Your First Markdown Component

Create a file `Pages/Hello.md` in your Blazor project:

```markdown
---
url: /hello
title: Hello Page
---

# Hello, Blazor Markdown!

Welcome to your first Markdown component. The current time is @DateTime.Now.
```

### Build the Project

```bash
dotnet build
```

**What Happens**:
- The generator processes `Hello.md`
- Generates `Hello.md.g.cs` with a `Hello` component class
- Markdown is converted to HTML
- `@DateTime.Now` is converted to a Blazor expression
- `@page "/hello"` directive is added from `url` YAML key

---

## Step 3: Run and Navigate

```bash
dotnet run
```

Navigate to `https://localhost:5001/hello` (adjust port as needed).

**You should see**:
- Heading: "Hello, Blazor Markdown!"
- Text: "Welcome to your first Markdown component. The current time is [timestamp]."
- Page title: "Hello Page" in browser tab

---

## Step 4: Add Component Parameters

Edit `Pages/Hello.md` to accept a parameter:

```markdown
---
url: /hello
title: Hello Page
$parameters:
  - Name: UserName
    Type: string
---

# Hello, @UserName!

Welcome to your first Markdown component. The current time is @DateTime.Now.
```

**Usage from another component**:

```razor
<Hello UserName="Alice" />
```

Or navigate to: `/hello?UserName=Alice` (Blazor auto-binds query strings)

---

## Step 5: Embed C# Code

Add logic with `@code` blocks:

```markdown
---
url: /hello
title: Hello Page
$parameters:
  - Name: UserName
    Type: string
---

# Hello, @UserName!

You have visited this page @visitCount times.

<button @onclick="IncrementCount">Increment</button>

@code {
    private int visitCount = 0;
    
    protected override void OnInitialized()
    {
        // Load visit count from localStorage or service
        visitCount = 1;
    }
    
    private void IncrementCount()
    {
        visitCount++;
    }
}
```

**What You Get**:
- Private field `visitCount`
- Lifecycle method `OnInitialized`
- Event handler `IncrementCount`
- Reactive UI updates when `visitCount` changes

---

## Step 6: Use Child Components

Reference existing Blazor components:

```markdown
---
url: /hello
title: Hello Page
---

# Hello, World!

<Counter />

<Alert Severity="Info">This is an informational alert!</Alert>
```

**Requirements**:
- `Counter` and `Alert` components must exist in your project
- Components are resolved by namespace (auto-imported or specify `$using`)

---

## YAML Front Matter Reference

### Common Keys

| Key | Example | Description |
|-----|---------|-------------|
| `url` | `url: /about` | Route for page. Generates `@page` directive. |
| `title` | `title: About Us` | Page title. Generates `<PageTitle>`. |
| `$parameters` | See below | Component parameters. |
| `$using` | `$using: [MyApp.Services]` | Using directives. |
| `$layout` | `$layout: MainLayout` | Layout component. |

### Parameter Definition

```yaml
$parameters:
  - Name: PostId
    Type: int
  - Name: Title
    Type: string
```

**Generates**:
```csharp
[Parameter]
public int PostId { get; set; }

[Parameter]
public string Title { get; set; } = default!;
```

### Multiple Routes

```yaml
url: [/, /home, /index]
```

**Generates**:
```csharp
[Route("/")]
[Route("/home")]
[Route("/index")]
```

---

## Razor Syntax Cheatsheet

### Inline Expressions

```markdown
Current user: @UserName
Items: @items.Count
Math: @(2 + 2)
```

### Conditional Rendering

```markdown
@if (isLoggedIn)
{
    <p>Welcome back!</p>
}
else
{
    <p>Please log in.</p>
}
```

### Loops

```markdown
@foreach (var item in items)
{
    <li>@item.Name</li>
}
```

### Component References

```markdown
<!-- Self-closing -->
<Counter />

<!-- With parameters -->
<Alert Severity="Warning" Message="@errorMessage" />

<!-- With child content -->
<Card Title="My Card">
    This is the body content.
</Card>
```

---

## Folder Structure & Namespaces

### Project Structure

```
MyBlazorApp/
├── Pages/
│   ├── Index.md          → MyBlazorApp.Pages.Index
│   └── Blog/
│       └── Post.md       → MyBlazorApp.Pages.Blog.Post
├── Components/
│   └── Shared/
│       └── Alert.md      → MyBlazorApp.Components.Shared.Alert
```

### Namespace Rules

**Default**: `<ProjectRootNamespace>.<FolderPath>`

**Override**: Use `$namespace` in YAML:

```yaml
$namespace: MyApp.CustomPages
```

---

## Hot Reload Support

**Changes to `.md` files trigger automatic regeneration.**

1. Edit `Hello.md`
2. Save the file
3. Generator runs automatically (on save or build)
4. Blazor hot reload updates the page (no manual refresh needed)

**Limitations**: Changes to `@code` blocks may require manual refresh in some cases.

---

## Debugging Generated Code

### View Generated Source

**Option 1: IDE**  
- Visual Studio: Right-click project → "Analyze and Code Cleanup" → "View Generated Files"
- Rider: Navigate to `obj/Debug/net8.0/generated/Markdn.SourceGenerators/`

**Option 2: File System**  
Generated files are at:
```
<ProjectRoot>/obj/<Configuration>/<TargetFramework>/generated/Markdn.SourceGenerators/
```

### Enable Verbose Build Output

Add to `.csproj`:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files will appear in `obj/GeneratedFiles/` with full diagnostics.

---

## Common Patterns

### Blog Post with Metadata

`Posts/MyPost.md`:
```markdown
---
url: /blog/my-post
title: My First Post
$parameters:
  - Name: Author
    Type: string
  - Name: PublishedDate
    Type: DateTime
---

# @Title

**By @Author** | @PublishedDate.ToShortDateString()

This is the blog post content written in Markdown.

<CommentSection PostId="my-post" />

@code {
    [Parameter]
    public string Title { get; set; } = "Untitled";
}
```

### Reusable Component (No Route)

`Components/AlertBox.md`:
```markdown
---
$parameters:
  - Name: Message
    Type: string
  - Name: Severity
    Type: AlertLevel
---

<div class="alert alert-@Severity.ToString().ToLower()">
    @Message
</div>

@code {
    public enum AlertLevel
    {
        Info,
        Warning,
        Error
    }
}
```

**Usage**:
```razor
<AlertBox Message="Save successful!" Severity="AlertLevel.Info" />
```

### Layout Page

`Shared/MainLayout.md`:
```markdown
---
$inherit: LayoutComponentBase
---

<header>
    <h1>My Site</h1>
</header>

<main>
    @Body
</main>

<footer>
    © 2024 My Company
</footer>

@code {
    // LayoutComponentBase provides the @Body property
}
```

---

## Troubleshooting

### Generator Not Running

**Symptoms**: No `.md.g.cs` files generated

**Solutions**:
1. Ensure `<ProjectReference>` has `OutputItemType="Analyzer"`
2. Clean and rebuild: `dotnet clean && dotnet build`
3. Check build output for diagnostic messages (MD001-MD008 codes)
4. Verify `.md` files have `Build Action: C# Analyzer Additional File` (should be automatic)

### Component Not Found

**Symptoms**: `CS0246: The type or namespace name 'MyComponent' could not be found`

**Solutions**:
1. Check component namespace matches folder structure
2. Add `@using` directive or specify `$using` in YAML
3. Verify the component is in the same project or referenced project

### Razor Syntax Errors

**Symptoms**: `MD007: Malformed Razor syntax`

**Solutions**:
1. Ensure `@code` blocks have matching braces
2. Check for unescaped `@` symbols (use `@@` to escape)
3. Validate YAML front matter syntax (use a YAML linter)

### Hot Reload Not Working

**Solutions**:
1. Stop and restart `dotnet watch`
2. Check if `@code` blocks changed (may require full restart)
3. Verify hot reload is enabled in `launchSettings.json`

---

## Next Steps

### Learn More

- **Full Specification**: See `specs/003-blazor-markdown-components/spec.md`
- **Contract Details**: See `contracts/component-generation-schema.md`
- **Data Model**: See `data-model.md`

### Advanced Topics

- Custom Markdown extensions (future)
- Integration with CMS systems
- Dynamic component loading
- Server-side rendering optimization

### Community & Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/markdn/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/markdn/discussions)
- **Documentation**: [Full Docs](https://docs.yoursite.com)

---

## Complete Example: Blog Post Page

`Pages/Blog/Post.md`:

```markdown
---
url: /blog/@Slug
title: "@Title - My Blog"
$namespace: MyApp.Pages.Blog
$using:
  - MyApp.Models
  - MyApp.Services
$parameters:
  - Name: Slug
    Type: string
  - Name: BlogService
    Type: IBlogService
---

# @post.Title

**By @post.Author** | @post.PublishedDate.ToShortDateString()

@((MarkupString)post.HtmlContent)

---

## Comments

@if (comments.Any())
{
    <CommentList Comments="@comments" />
}
else
{
    <p>No comments yet. Be the first!</p>
}

<CommentForm OnCommentAdded="LoadComments" />

@code {
    private BlogPost post = new();
    private List<Comment> comments = new();
    
    protected override async Task OnInitializedAsync()
    {
        post = await BlogService.GetPostBySlugAsync(Slug);
        await LoadComments();
    }
    
    private async Task LoadComments()
    {
        comments = await BlogService.GetCommentsAsync(post.Id);
    }
}
```

**Result**: Fully functional blog post page with:
- Dynamic routing by slug
- Dependency injection (`BlogService`)
- Async initialization
- Conditional rendering
- Child components (`CommentList`, `CommentForm`)
- Event handling (`OnCommentAdded`)

---

**You're ready to build Markdown-powered Blazor applications!** 🚀
