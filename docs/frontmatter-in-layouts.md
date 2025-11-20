# Passing Frontmatter Data to Layouts

This guide explains how to pass frontmatter data from markdown pages to Blazor layouts, enabling layouts to access and display page metadata like tags, publication date, author, and more.

## Overview

When markdown files are converted to Razor components, frontmatter data (title, tags, date, etc.) is automatically made available to layouts through Blazor's cascading parameters mechanism. This allows layouts to display page-specific metadata without duplicating information.

## How It Works

1. **Automatic Generation**: The markdown-to-Razor generator automatically creates a `PageMetadata` class and instance in each generated component
2. **Cascading Value**: The page content is wrapped in a `<CascadingValue>` component that provides the metadata
3. **Layout Access**: Layouts can receive this metadata using a `[CascadingParameter]`

## Generated Code

For a markdown file with frontmatter like this:

```markdown
---
title: "My Blog Post"
slug: my-blog-post
date: 2025-11-20
tags: ["blazor", "markdown", "cms"]
summary: "An introduction to using Markdn"
---

# My Blog Post

Content here...
```

The generator creates a Razor component with:

```razor
@page "/my-blog-post"

<PageTitle>My Blog Post</PageTitle>

<CascadingValue Value="@_pageMetadata">
  <!-- Page content here -->
</CascadingValue>

@code {
    private class PageMetadata
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Route { get; set; }
        public string? Summary { get; set; }
        public DateTime? Date { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    private readonly PageMetadata _pageMetadata = new()
    {
        Title = "My Blog Post",
        Slug = "my-blog-post",
        Route = "/my-blog-post",
        Summary = "An introduction to using Markdn",
        Date = DateTime.Parse("2025-11-20T00:00:00.0000000"),
        Tags = new List<string> { "blazor", "markdown", "cms" },
    };
}
```

## Using Metadata in Layouts

To access frontmatter data in your layout, add a cascading parameter:

```razor
@inherits LayoutComponentBase

@code {
    private dynamic? _pageMetadata;

    [CascadingParameter]
    private dynamic? PageMetadata
    {
        get => _pageMetadata;
        set => _pageMetadata = value;
    }
}
```

### Complete Layout Example

Here's a complete layout that displays page metadata:

```razor
@inherits LayoutComponentBase

<div class="page">
    <header class="page-header">
        @if (_pageMetadata != null)
        {
            <h1>@_pageMetadata.Title</h1>
            
            @if (_pageMetadata.Date != null)
            {
                <p class="date">
                    Published: @_pageMetadata.Date.Value.ToString("MMMM dd, yyyy")
                </p>
            }
            
            @if (_pageMetadata.Summary != null)
            {
                <p class="summary">@_pageMetadata.Summary</p>
            }
            
            @if (_pageMetadata.Tags != null && _pageMetadata.Tags.Count > 0)
            {
                <div class="tags">
                    <strong>Tags:</strong>
                    @foreach (var tag in _pageMetadata.Tags)
                    {
                        <span class="tag">@tag</span>
                    }
                </div>
            }
        }
    </header>

    <main>
        @Body
    </main>
</div>

@code {
    private dynamic? _pageMetadata;

    [CascadingParameter]
    private dynamic? PageMetadata
    {
        get => _pageMetadata;
        set => _pageMetadata = value;
    }
}
```

## Available Metadata Fields

The `PageMetadata` class includes the following fields:

- **Title** (`string?`): The page title from frontmatter
- **Slug** (`string?`): The page slug/identifier
- **Route** (`string?`): The route/URL of the page
- **Summary** (`string?`): A brief description or summary
- **Date** (`DateTime?`): Publication or creation date
- **Tags** (`List<string>?`): List of tags associated with the page
- **AdditionalData** (`Dictionary<string, object>?`): Reserved for future use with custom frontmatter fields

## Use Cases

This feature enables many common CMS scenarios:

### 1. Blog Post Metadata Display
Display publication date, author, reading time, and tags at the top of each post.

### 2. Breadcrumb Navigation
Use the route and title to generate breadcrumb navigation.

### 3. Related Content
Use tags to find and display related articles.

### 4. SEO Metadata
Pass summary and tags to generate meta tags for SEO.

### 5. Category Filtering
Use tags or categories to organize and filter content.

## Example: Blog Layout

```razor
@inherits LayoutComponentBase

<div class="blog-post">
    @if (_pageMetadata != null)
    {
        <article>
            <header class="post-header">
                <h1>@_pageMetadata.Title</h1>
                
                <div class="post-meta">
                    @if (_pageMetadata.Date != null)
                    {
                        <time datetime="@_pageMetadata.Date.Value.ToString("yyyy-MM-dd")">
                            @_pageMetadata.Date.Value.ToString("MMMM dd, yyyy")
                        </time>
                    }
                    
                    @if (_pageMetadata.Tags != null && _pageMetadata.Tags.Count > 0)
                    {
                        <div class="tags">
                            @foreach (var tag in _pageMetadata.Tags)
                            {
                                <a href="/tags/@tag" class="tag">@tag</a>
                            }
                        </div>
                    }
                </div>
            </header>

            <div class="post-content">
                @Body
            </div>
            
            @if (_pageMetadata.Tags != null && _pageMetadata.Tags.Count > 0)
            {
                <footer class="post-footer">
                    <h3>Topics covered</h3>
                    <ul>
                        @foreach (var tag in _pageMetadata.Tags)
                        {
                            <li><a href="/tags/@tag">@tag</a></li>
                        }
                    </ul>
                </footer>
            }
        </article>
    }
</div>

@code {
    private dynamic? _pageMetadata;

    [CascadingParameter]
    private dynamic? PageMetadata
    {
        get => _pageMetadata;
        set => _pageMetadata = value;
    }
}
```

## Conditional Rendering

Since metadata fields are nullable, always check for null before using them:

```razor
@if (_pageMetadata?.Date != null)
{
    <p>Published: @_pageMetadata.Date.Value.ToString("D")</p>
}

@if (_pageMetadata?.Tags?.Count > 0)
{
    <div class="tags">
        @foreach (var tag in _pageMetadata.Tags)
        {
            <span>@tag</span>
        }
    </div>
}
```

## Best Practices

1. **Always check for null**: Metadata fields are optional and may be null
2. **Use meaningful layouts**: Create different layouts for different content types (blog posts, documentation, etc.)
3. **Consistent frontmatter**: Establish conventions for frontmatter fields across your content
4. **Type safety**: Use `dynamic` for the cascading parameter to access the locally-defined `PageMetadata` class

## Testing

You can test this feature by creating a markdown file with frontmatter and viewing it with a layout that displays the metadata:

1. Create a markdown file with frontmatter:
   ```markdown
   ---
   title: "Test Page"
   date: 2025-11-20
   tags: ["test", "demo"]
   ---
   
   # Test Content
   ```

2. Specify a layout that uses metadata:
   ```markdown
   ---
   layout: MyApp.Layouts.MetadataLayout
   ---
   ```

3. Build and run your application to see the metadata displayed in the layout

## See Also

- [Blazor Layouts Documentation](https://docs.microsoft.com/aspnet/core/blazor/components/layouts)
- [Blazor Cascading Values and Parameters](https://docs.microsoft.com/aspnet/core/blazor/components/cascading-values-and-parameters)
- Markdn Content Collections Documentation
