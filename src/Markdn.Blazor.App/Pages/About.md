---
url: /about-legacy
title: About This Project
---

# About This Project

Welcome to the **Markdn Blazor Components** demo!

## What is This?

This page is written entirely in Markdown and automatically converted to a Blazor component.

## Features

- Write content in Markdown
- Automatic conversion to Blazor components
- Full Blazor integration
- **No** manual HTML required

### Code Example

```csharp
public class MarkdownComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddMarkupContent(0, htmlContent);
    }
}
```

## Benefits

1. **Simple**: Just write Markdown
2. **Fast**: Compile-time generation
3. **Type-safe**: Full C# integration

---

*Generated from Markdown at build time* ✨
