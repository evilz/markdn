# Testing Markdown Components - Route Options

## Current Status

✅ **Option 2 (Razor Wrapper with DynamicComponent) - WORKING NOW**
⏳ **Option 1 (YAML Front Matter) - NOT YET IMPLEMENTED** (Task T015)

**Update 2025-11-10**: Successfully resolved component rendering issues. Use `<DynamicComponent Type="typeof(...)" />` for reliable component loading.

---

## Option 2: Razor Wrapper Pages (Working Now) ✅

Create a `.razor` file that wraps your `.md` component with a route.

### Example Files Created:

**1. GreetingPage.razor** (`Components/Pages/GreetingPage.razor`)
```razor
@page "/greeting"

<DynamicComponent Type="typeof(Markdn.Blazor.App.Pages.Greeting)" />
```

**2. OnlyMarkPage.razor** (`Components/Pages/OnlyMarkPage.razor`)
```razor
@page "/onlymd"

<DynamicComponent Type="typeof(Markdn.Blazor.App.Pages.OnlyMark)" />
```

**3. AboutPage.razor** (`Components/Pages/AboutPage.razor`)
```razor
@page "/about"

<DynamicComponent Type="typeof(Markdn.Blazor.App.Pages.About)" />
```

**Note**: Using `<DynamicComponent>` ensures reliable component loading. Direct component references like `<OnlyMark />` may render as custom HTML elements instead of Blazor components.

### Test Routes:
- http://localhost:5076/greeting (Greeting component)
- http://localhost:5076/onlymd (Simple Hello World)
- http://localhost:5076/about (Project description with features)
- http://localhost:5076/features (Markdown capabilities showcase)

### Pros:
- ✅ Works immediately with current implementation
- ✅ Full control over route and page title
- ✅ Can add additional Blazor components or logic
- ✅ Reliable component loading with `<DynamicComponent>`
- ✅ No namespace resolution issues

### Cons:
- ⚠️ Requires separate `.razor` file for each routable page
- ⚠️ Two files to maintain per page
- ⚠️ Slightly more verbose than YAML front matter approach

---

## Option 1: YAML Front Matter (Coming Soon) ⏳

Add front matter directly to `.md` files to specify routes.

### Example: OnlyMark.md
```markdown
---
url: /onlymd
---

# Hello, World!

## cool !!
```

### Current Behavior:
❌ YAML front matter is rendered as plain text:
```html
<p>---</p>
<p>url: /onlymd</p>
<p>---</p>
<h1>Hello, World!</h1>
```

### What's Needed:
1. Implement **YamlFrontMatterParser** (Task T015)
2. Parse `url` property and generate `[Route("/onlymd")]` attribute
3. Strip YAML from Markdown content before HTML conversion

### Implementation Tasks:
- [ ] T015: Create YamlFrontMatterParser
- [ ] T041-T052: User Story 2 - Route generation from metadata

### Once Implemented:
✅ Single `.md` file per page
✅ No wrapper files needed
✅ Clean separation of content and metadata

---

## Quick Start Guide

### Using Option 2 (Available Now):

1. **Create your Markdown file** in `Pages/`:
   ```markdown
   # My Page

   This is my content.
   ```
   Saved as: `Pages/MyPage.md`

2. **Create a wrapper page** in `Components/Pages/`:
   ```razor
   @page "/mypage"

   <DynamicComponent Type="typeof(Markdn.Blazor.App.Pages.MyPage)" />
   ```
   Saved as: `Components/Pages/MyPageRoute.razor`

3. **Build and run**:
   ```bash
   dotnet build
   dotnet run
   ```

4. **Navigate to**: http://localhost:5076/mypage

### What Gets Generated:

`Pages/MyPage.md` → `obj/Generated/.../MyPage.md.g.cs`:
```csharp
namespace Markdn.Blazor.App.Pages
{
    public partial class MyPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddMarkupContent(0, @"<h1>My Page</h1>...");
        }
    }
}
```

---

## Roadmap

**Phase 3 (US1)**: ✅ Simple Markdown Components - **COMPLETE**
- Basic Markdown → Blazor component generation
- Works with Option 2 (wrapper pages)

**Phase 5 (US2)**: ⏳ Route Generation - **PLANNED**
- YAML front matter parsing
- Automatic route generation
- Enables Option 1 (self-routed .md files)

**Current Recommendation**: Use Option 2 for immediate results. Once Task T015 is complete, you can migrate to Option 1 by removing wrapper files and adding YAML front matter to your `.md` files.
