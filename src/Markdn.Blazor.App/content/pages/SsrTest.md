# SSR Test Page

This page tests **Static Server-Side Rendering** (SSR).

## Render Information

- **Render Mode**: Static SSR (no interactivity)
- **Generated At**: Build time
- **Rendered On**: Server only

## Features Working in SSR

- ✅ Headings (H1-H6)
- ✅ **Bold** and *italic* text
- ✅ Lists and ~~strikethrough~~
- ✅ [Links](https://blazor.net)
- ✅ `Inline code`

### Code Block

```csharp
// This is rendered server-side
var mode = "SSR";
Console.WriteLine($"Render mode: {mode}");
```

## SSR Benefits

1. **Fast initial load** - Pre-rendered HTML
2. **SEO friendly** - Content visible to crawlers
3. **No client-side runtime** - Smaller payload

---

*This content is generated from Markdown and rendered as static HTML on the server.*
