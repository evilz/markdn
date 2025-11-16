# Source-Generated Content Collections

Markdn provides a compile-time source generator that creates type-safe content services from content files, inspired by [Astro's Content Collections API](https://docs.astro.build/en/guides/content-collections/).

## Overview

The **ContentCollectionsGenerator** is a C# source generator that:
- Works with multiple file formats: Markdown (`.md`), YAML (`.yaml`, `.yml`), TOML (`.toml`), and JSON (`.json`)
- Generates typed C# classes for collections using the `[Collection]` attribute
- Creates service classes with `GetCollection()` and `GetEntry(slug)` methods
- Provides compile-time type safety for content properties
- Automatically generates slugs from file paths when not explicitly provided
- Optionally generates minimal Web API endpoints for collections
- For Markdown files, generates Blazor components with automatic routing

This feature is ideal for **Blazor applications** where you want type-safe access to content with IntelliSense support.

## Quick Start

### 1. Install the Source Generator

Add the `Markdn.SourceGenerators` package to your Blazor project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Markdn.SourceGenerators/Markdn.SourceGenerators.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false"/>
</ItemGroup>
```

### 2. Define Your Content Model

Create a C# class or record with the `[Collection]` attribute:

```csharp
using Markdn.Content;

[Collection("Content/posts/**/*.md", Name = "posts")]
public class Post
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public DateTime PubDate { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public List<string> Tags { get; init; } = new();
    public string Content { get; init; } = string.Empty;
}
```

### 3. Configure Your Project

Add content files to your `.csproj`:

```xml
<ItemGroup>
  <!-- Include content files as embedded resources -->
  <EmbeddedResource Include="Content\**\*.md" />
  <EmbeddedResource Include="Content\**\*.yaml" />
  <EmbeddedResource Include="Content\**\*.yml" />
  <EmbeddedResource Include="Content\**\*.toml" />
  <EmbeddedResource Include="Content\**\*.json" />
</ItemGroup>
```

### 4. Create Content

Create content files in your content folder:

**Content/posts/getting-started.md:**
```markdown
---
title: "Getting Started with Markdn"
pubDate: 2025-11-01T10:00:00Z
description: "Learn how to get started with Markdn"
author: "John Doe"
tags: ["tutorial", "getting-started"]
---

# Getting Started

Your markdown content here...
```

### 5. Use the Generated Service

The source generator creates type-safe services in the `{YourNamespace}.Content` namespace:

```csharp
using YourApp.Content;

public class BlogService
{
    private readonly IPostsService _postsService;

    public BlogService()
    {
        _postsService = new PostsService();
    }

    public void DisplayPosts()
    {
        // GetCollection - returns all posts
        var allPosts = _postsService.GetCollection();
        
        foreach (var post in allPosts)
        {
            // Type-safe property access with IntelliSense!
            Console.WriteLine($"{post.Title} by {post.Author}");
            Console.WriteLine($"Published: {post.PubDate:yyyy-MM-dd}");
            Console.WriteLine($"Description: {post.Description}");
        }

        // GetEntry - get a specific post by slug
        var specificPost = _postsService.GetEntry("getting-started");
        if (specificPost != null)
        {
            Console.WriteLine(specificPost.Content); // Full markdown content
        }
    }
}
```

## API Reference

### Generated Interface

For each collection, the generator creates an interface:

```csharp
public interface IPostsService
{
    /// <summary>Gets all items from the 'posts' collection (getCollection).</summary>
    List<PostsEntry> GetCollection();

    /// <summary>Gets a single item from the 'posts' collection by slug (getEntry).</summary>
    PostsEntry? GetEntry(string slug);
}
```

### Generated Entry Class

Based on your schema, a typed entry class is generated:

```csharp
public class PostsEntry
{
    /// <summary>The unique slug/identifier for this entry.</summary>
    public required string Slug { get; init; }

    /// <summary>The markdown content body.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Title from frontmatter.</summary>
    public required string Title { get; init; }

    /// <summary>PubDate from frontmatter.</summary>
    public required DateTime PubDate { get; init; }

    /// <summary>Description from frontmatter.</summary>
    public string? Description { get; init; }

    /// <summary>Author from frontmatter.</summary>
    public string? Author { get; init; }

    /// <summary>Tags from frontmatter.</summary>
    public List<string> Tags { get; init; } = new();
}
```

### Generated Service Class

The implementation handles loading and parsing:

```csharp
public class PostsService : IPostsService
{
    private readonly Lazy<List<PostsEntry>> _entries;

    public PostsService()
    {
        _entries = new Lazy<List<PostsEntry>>(() => LoadCollection());
    }

    public List<PostsEntry> GetCollection()
    {
        return _entries.Value;
    }

    public PostsEntry? GetEntry(string slug)
    {
        return _entries.Value.FirstOrDefault(e => 
            e.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    // Internal: Loads from embedded resources and parses frontmatter
    private List<PostsEntry> LoadCollection() { /* ... */ }
}
```

## New Features

### Multi-Format Support

The source generator now supports multiple file formats beyond Markdown:

**Supported Formats:**
- **Markdown** (`.md`) - YAML front-matter + content body, generates Blazor components
- **YAML** (`.yaml`, `.yml`) - Direct data files
- **TOML** (`.toml`) - Configuration-friendly format
- **JSON** (`.json`) - Standard JSON data files

**Example YAML file (`Content/data/authors.yaml`):**
```yaml
slug: john-doe
name: John Doe
email: john@example.com
bio: Senior developer and content creator
```

**Example JSON file (`Content/data/config.json`):**
```json
{
  "slug": "site-config",
  "siteName": "My Blog",
  "theme": "dark",
  "enableComments": true
}
```

### Auto-Generated Slugs

If your content files don't have an explicit `slug` property, the generator automatically creates one from the file path:

```
Content/posts/getting-started.md        → slug: "content-posts-getting-started"
Content/blog/2024/my-article.md         → slug: "content-blog-2024-my-article"
Data/authors/john-doe.yaml              → slug: "data-authors-john-doe"
```

The slug is:
- Converted to lowercase
- Path separators replaced with hyphens
- Special characters normalized
- File extension removed

### WebAPI Endpoint Generation

Automatically generate minimal Web API endpoints for your collections:

```csharp
[Collection("Content/posts/**/*.md", Name = "posts", GenerateWebApi = true)]
public class Post
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    // ... other properties
}
```

This generates:
- `GET /api/collections/posts/items` - Get all posts
- `GET /api/collections/posts/items/{slug}` - Get post by slug

The endpoints are minimal and follow REST conventions. They're automatically registered in your application startup.

### Dynamic Content Support

For schema-less or flexible content, use C# `dynamic`:

```csharp
[Collection("Content/data/**/*.json", Name = "dynamic")]
public class DynamicContent
{
    public required string Slug { get; init; }
    public required dynamic Data { get; init; }
}

// Usage
var service = new DynamicService();
var item = service.GetEntry("my-item");
var anyProperty = item.Data.customField; // Access any property
```

### Blazor Component Generation (Markdown Only)

For Markdown files, the generator creates Blazor components:

```csharp
[Collection("Content/pages/**/*.md", Name = "pages")]
public class Page
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string Content { get; init; } = string.Empty;
}

// Use the generated service
var service = new PagesService();

// Get the data
var page = service.GetEntry("about");

// Get the Blazor component (RenderFragment)
var component = service.GetComponent("about");
```

If the Markdown file has a `slug` property in its front-matter, the generated component becomes a routable Blazor page with `@page "/{slug}"`.

## Schema Configuration

### Collection Structure

```json
{
  "collections": {
    "collection-name": {
      "folder": "relative/path/to/content",
      "schema": {
        "type": "object",
        "properties": {
          // Field definitions
        },
        "required": ["field1", "field2"]
      }
    }
  }
}
```

### Supported Field Types

| JSON Schema Type | C# Type | Example |
|-----------------|---------|---------|
| `string` | `string` | `"Hello"` |
| `string` with `format: "date-time"` | `DateTime` | `2025-11-01T10:00:00Z` |
| `number` | `double` | `3.14` |
| `integer` | `int` | `42` |
| `boolean` | `bool` | `true` |
| `array` | `List<string>` | `["tag1", "tag2"]` |

### Required vs Optional Fields

**Required fields** (in the `required` array):
```json
{
  "properties": {
    "title": { "type": "string" }
  },
  "required": ["title"]
}
```
Generates: `public required string Title { get; init; }`

**Optional fields** (not in `required`):
```json
{
  "properties": {
    "description": { "type": "string" }
  }
}
```
Generates: `public string? Description { get; init; }`

### DateTime Format

Use `format: "date-time"` for DateTime fields:

```json
{
  "pubDate": {
    "type": "string",
    "format": "date-time"
  }
}
```

Frontmatter:
```yaml
pubDate: 2025-11-01T10:00:00Z
```

Access:
```csharp
DateTime date = post.PubDate;
```

## Advanced Usage

### Multiple Collections

Define multiple collections in `collections.json`:

```json
{
  "collections": {
    "posts": {
      "folder": "Content/posts",
      "schema": { /* ... */ }
    },
    "docs": {
      "folder": "Content/docs",
      "schema": { /* ... */ }
    },
    "products": {
      "folder": "Content/products",
      "schema": { /* ... */ }
    }
  }
}
```

Each generates its own service:
- `IPostsService` / `PostsService` / `PostsEntry`
- `IDocsService` / `DocsService` / `DocsEntry`
- `IProductsService` / `ProductsService` / `ProductsEntry`

### Filtering and Querying

Use LINQ to query collections:

```csharp
var postsService = new PostsService();

// Filter by author
var authorPosts = postsService.GetCollection()
    .Where(p => p.Author == "John Doe")
    .ToList();

// Sort by date
var recentPosts = postsService.GetCollection()
    .OrderByDescending(p => p.PubDate)
    .Take(5)
    .ToList();

// Complex queries
var filteredPosts = postsService.GetCollection()
    .Where(p => p.PubDate > DateTime.Now.AddMonths(-1))
    .Where(p => p.Tags.Contains("tutorial"))
    .OrderByDescending(p => p.PubDate)
    .ToList();
```

### Dependency Injection

Register services in your DI container:

```csharp
// Program.cs
builder.Services.AddSingleton<IPostsService, PostsService>();

// Component.razor
@inject IPostsService PostsService

@code {
    protected override void OnInitialized()
    {
        var posts = PostsService.GetCollection();
    }
}
```

### Custom Wrapper Services

Create wrapper services for additional logic:

```csharp
public interface IBlogService
{
    List<Post> GetAllPosts();
    Post? GetPostBySlug(string slug);
    List<Post> GetRecentPosts(int count);
}

public class BlogService : IBlogService
{
    private readonly IPostsService _contentService;

    public BlogService()
    {
        _contentService = new PostsService();
    }

    public List<Post> GetAllPosts()
    {
        return _contentService.GetCollection()
            .Select(MapToPost)
            .ToList();
    }

    public Post? GetPostBySlug(string slug)
    {
        var entry = _contentService.GetEntry(slug);
        return entry != null ? MapToPost(entry) : null;
    }

    public List<Post> GetRecentPosts(int count)
    {
        return _contentService.GetCollection()
            .OrderByDescending(p => p.PubDate)
            .Take(count)
            .Select(MapToPost)
            .ToList();
    }

    private Post MapToPost(PostsEntry entry)
    {
        return new Post
        {
            Slug = entry.Slug,
            Title = entry.Title,
            PubDate = entry.PubDate,
            Description = entry.Description,
            Content = entry.Content
        };
    }
}
```

## Comparison: Source Generator vs Runtime API

| Feature | Source Generator | Runtime API |
|---------|-----------------|-------------|
| **Type Safety** | ✅ Compile-time | ❌ Runtime strings |
| **IntelliSense** | ✅ Full support | ❌ No support |
| **Performance** | ✅ No parsing overhead | ⚠️ Runtime parsing |
| **Use Case** | Blazor apps | REST API consumers |
| **Validation** | ⚠️ Schema at compile-time only | ✅ Full runtime validation |
| **Querying** | ⚠️ LINQ only | ✅ OData-style syntax |
| **Setup** | Simple (just add generator) | Requires API server |

### When to Use Source Generator

✅ **Use the source generator when:**
- Building a Blazor application (Server or WebAssembly)
- You want compile-time type safety
- You want IntelliSense for frontmatter properties
- Content is embedded in your application
- You prefer LINQ for querying

### When to Use Runtime API

✅ **Use the runtime API when:**
- Building a separate content service/microservice
- Content needs to be updated without recompiling
- You need advanced OData-style queries
- Multiple applications need to share content
- You need runtime schema validation

### Using Both Together

You can use both! The source generator is perfect for **Blazor frontends** while the runtime API serves **external consumers**:

```
┌─────────────────┐
│  Blazor App     │  ← Uses source generator (compile-time types)
└────────┬────────┘
         │
         ├─────────────────┐
         ↓                 ↓
┌─────────────────┐  ┌──────────────┐
│ Content Files   │  │ Markdn API   │  ← Uses runtime collections
└─────────────────┘  └──────┬───────┘
                            ↓
                     ┌──────────────┐
                     │ Mobile App   │  ← Consumes REST API
                     └──────────────┘
```

## Troubleshooting

### Generator Not Running

**Problem:** Generated code not appearing.

**Solutions:**
1. Ensure `collections.json` is included as `<AdditionalFiles>`:
   ```xml
   <AdditionalFiles Include="collections.json" />
   ```

2. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

3. Check for build errors in the output window

### No IntelliSense for Generated Types

**Problem:** Can't see `PostsEntry` or `IPostsService`.

**Solutions:**
1. Rebuild the project completely
2. Restart your IDE (VS Code, Visual Studio, Rider)
3. Check the namespace - generated types are in `{YourRootNamespace}.Content`

### Markdown Files Not Loading

**Problem:** `GetCollection()` returns empty list.

**Solutions:**
1. Ensure markdown files are included as embedded resources:
   ```xml
   <EmbeddedResource Include="Content\**\*.md" />
   ```

2. Check the `folder` path in `collections.json` matches your actual folder structure

3. Verify frontmatter is valid YAML starting with `---`

### Type Mismatch Errors

**Problem:** Compilation errors about missing required properties.

**Solutions:**
1. Ensure required fields in schema match your markdown frontmatter
2. Check date formats match `format: "date-time"` for DateTime fields
3. Make optional fields nullable in your code if they're not in the `required` array

### Array Fields Are Empty

**Known Limitation:** Array/list fields (like `tags`) are currently initialized as empty lists.

**Workaround:** Array parsing from YAML frontmatter is planned for a future update. For now, arrays will always be empty in generated entries.

## Examples

### Blog Application

See the complete example in `samples/Markdn.Pico`:

```
samples/Markdn.Pico/
├── Markdn.Pico/
│   ├── collections.json          # Collection configuration
│   ├── Content/
│   │   └── posts/
│   │       ├── getting-started.md
│   │       ├── building-a-blog.md
│   │       └── advanced-features.md
│   ├── Services/
│   │   └── PostsService.cs       # Wrapper service
│   └── Components/
│       └── LatestPosts.razor     # Component using posts
```

**collections.json:**
```json
{
  "collections": {
    "posts": {
      "folder": "Content/posts",
      "schema": {
        "properties": {
          "title": { "type": "string" },
          "pubDate": { "type": "string", "format": "date-time" },
          "description": { "type": "string" }
        },
        "required": ["title", "pubDate"]
      }
    }
  }
}
```

**LatestPosts.razor:**
```razor
@using YourApp.Content
@inject IPostsService PostsService

<h2>Latest Posts</h2>
<ul>
    @foreach (var post in RecentPosts)
    {
        <li>
            <a href="/posts/@post.Slug">@post.Title</a>
            <time>@post.PubDate.ToString("MMMM dd, yyyy")</time>
        </li>
    }
</ul>

@code {
    private List<PostsEntry> RecentPosts = new();

    protected override void OnInitialized()
    {
        RecentPosts = PostsService.GetCollection()
            .OrderByDescending(p => p.PubDate)
            .Take(5)
            .ToList();
    }
}
```

## Performance Considerations

### Lazy Loading

Collections are loaded lazily on first access:

```csharp
public PostsService()
{
    // Not loaded yet
    _entries = new Lazy<List<PostsEntry>>(() => LoadCollection());
}

public List<PostsEntry> GetCollection()
{
    // Loaded only on first call
    return _entries.Value;
}
```

### Caching

Services use lazy initialization, so collections are loaded once and cached in memory. For large collections (100+ files), this is efficient since:

- Loading happens once per application lifetime
- Subsequent calls return cached data
- No disk I/O after initial load

### Memory Usage

Consider memory usage for large collections:
- Each markdown file is loaded into memory
- Parsed entries are kept in memory
- For 1000 blog posts (~5KB each): ~5MB memory

For very large collections, consider:
1. Splitting into multiple collections
2. Implementing your own caching strategy
3. Using the runtime API with database storage instead

## Migration Guide

### From Manual Markdown Parsing

**Before:**
```csharp
public class PostsService
{
    public List<Post> GetAllPosts()
    {
        // Manual file system access
        var files = Directory.GetFiles("Content/posts", "*.md");
        var posts = new List<Post>();
        
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var post = ParseMarkdown(content); // Manual parsing
            posts.Add(post);
        }
        
        return posts;
    }
}
```

**After:**
```csharp
public class PostsService
{
    private readonly IPostsService _contentService;
    
    public PostsService()
    {
        _contentService = new Content.PostsService();
    }
    
    public List<Post> GetAllPosts()
    {
        // Type-safe, auto-parsed
        return _contentService.GetCollection()
            .Select(e => new Post {
                Slug = e.Slug,
                Title = e.Title,
                PubDate = e.PubDate
            })
            .ToList();
    }
}
```

### From Runtime Collections API

If you're using the runtime API and want to add a Blazor frontend:

1. Keep the runtime API for external consumers
2. Add the source generator to your Blazor project
3. Copy `collections.json` from your API project
4. Add markdown files as embedded resources
5. Use the generated services in your Blazor components

Both can coexist and share the same schema definition!

## FAQ

### Q: Can I use this without Blazor?

**A:** Yes! The source generator works with any C# project (Console, WPF, WinForms, etc.). It's not Blazor-specific.

### Q: Do I need the Markdn.Api project?

**A:** No. The source generator is completely independent and doesn't require the API project or runtime.

### Q: Can I customize the generated code?

**A:** The generated code is read-only (`// <auto-generated>`). Instead, create wrapper services or extension methods.

### Q: How do I debug generated code?

**A:** 
1. Check `obj/Debug/{tfm}/generated/Markdn.SourceGenerators/` folder
2. Generated files have names like `ContentCollections.g.cs`
3. You can step into generated code during debugging

### Q: Does this work with .NET 6/7/8?

**A:** Yes! The source generator targets `netstandard2.0` and works with .NET 6+.

### Q: What about Razor/Blazor components in markdown?

**A:** This generator is for **content only**. For Razor components in markdown, use the `MarkdownComponentGenerator` instead.

## Related Documentation

- [Runtime Content Collections API](../README.md#content-collections) - REST API for collections
- [Astro Content Collections](https://docs.astro.build/en/guides/content-collections/) - Inspiration for this feature
- [JSON Schema](https://json-schema.org/) - Schema validation format
- [C# Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) - How source generators work

## Support

- 🐛 [Report Issues](https://github.com/evilz/markdn/issues)
- 💡 [Request Features](https://github.com/evilz/markdn/issues/new)
- 📖 [View Examples](../../samples/Markdn.Pico)
