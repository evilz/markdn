# SectionOutlet Support in Markdown Files

## Overview

The Markdown to Razor generator now supports Blazor's `SectionOutlet` and `SectionContent` components, allowing you to define content sections in your Markdown files that can be rendered in specific areas of your layout.

## What are SectionOutlet and SectionContent?

In Blazor, `SectionOutlet` and `SectionContent` are components that enable a parent component (like a layout) to define named outlets where child components (like pages) can inject content.

### Layout Example

```razor
@inherits LayoutComponentBase

<div class="page">
    <header>
        <SectionOutlet SectionName="PageHeader" />
    </header>
    
    <main>
        @Body
    </main>
    
    <aside>
        <SectionOutlet SectionName="Sidebar" />
    </aside>
</div>
```

### Traditional Razor Page

```razor
@page "/example"

<SectionContent SectionName="PageHeader">
    <h1>Custom Header</h1>
</SectionContent>

<SectionContent SectionName="Sidebar">
    <nav>Navigation content</nav>
</SectionContent>

<article>
    Main page content
</article>
```

## Using Sections in Markdown

You can now achieve the same result using Markdown files!

### Markdown Syntax

Use `<Section Name="sectionname">` tags to define sections in your Markdown:

```markdown
---
title: My Page
layout: Markdn.Blazor.App.Components.Layout.MyLayout
---

<Section Name="PageHeader">
# Welcome to My Page
This content will appear in the PageHeader section of the layout.
</Section>

<Section Name="Sidebar">
## Quick Links
- [Home](/)
- [About](/about)
- [Contact](/contact)
</Section>

# Main Content

This is the main content of the page. It will appear in the @Body section of the layout.

## Features

- Full markdown support everywhere
- Clean separation of concerns
- Flexible layouts
```

### Generated Razor Output

The above Markdown is converted to:

```razor
@page "/my-page"
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web

@layout Markdn.Blazor.App.Components.Layout.MyLayout

<PageTitle>My Page</PageTitle>

<SectionContent SectionName="PageHeader">
<h1 id="welcome-to-my-page">Welcome to My Page</h1>
<p>This content will appear in the PageHeader section of the layout.</p>
</SectionContent>

<SectionContent SectionName="Sidebar">
<h2 id="quick-links">Quick Links</h2>
<ul>
<li><a href="/">Home</a></li>
<li><a href="/about">About</a></li>
<li><a href="/contact">Contact</a></li>
</ul>
</SectionContent>

<h1 id="main-content">Main Content</h1>
<p>This is the main content of the page. It will appear in the @Body section of the layout.</p>
<h2 id="features">Features</h2>
<ul>
<li>Full markdown support everywhere</li>
<li>Clean separation of concerns</li>
<li>Flexible layouts</li>
</ul>
```

## Key Features

### 1. Full Markdown Support

All markdown features work inside sections:

```markdown
<Section Name="sidebar">
## Formatted Content

- **Bold items**
- *Italic text*
- [Links](http://example.com)
- `inline code`

```code blocks```
</Section>
```

### 2. Multiple Sections

Define as many sections as you need:

```markdown
<Section Name="header">
Header content
</Section>

<Section Name="sidebar">
Sidebar content
</Section>

<Section Name="footer">
Footer content
</Section>

Main content here
```

### 3. Code Block Safety

Section tags inside code blocks are ignored (not parsed as sections):

```markdown
Here's how to use sections:

```text
<Section Name="example">
This won't be parsed as a section
</Section>
```

<Section Name="actual">
But this will be!
</Section>
```

### 4. Case Insensitive

Tags are case-insensitive:

```markdown
<Section Name="test">...</Section>
<section Name="test">...</section>
<SECTION Name="test">...</SECTION>
```

All work the same way.

### 5. Flexible Quotes

Use single or double quotes for the Name attribute:

```markdown
<Section Name="test">...</Section>
<Section Name='test'>...</Section>
```

Both are valid.

## Best Practices

### 1. Use Descriptive Section Names

```markdown
<!-- Good -->
<Section Name="PageHeader">...</Section>
<Section Name="Sidebar">...</Section>
<Section Name="CallToAction">...</Section>

<!-- Avoid -->
<Section Name="s1">...</Section>
<Section Name="section2">...</Section>
```

### 2. Keep Main Content Separate

Put your primary content outside of sections:

```markdown
<Section Name="header">
Secondary content for header
</Section>

# Main Article Title

Your primary content goes here, outside of sections.
This appears in the @Body of the layout.

<Section Name="sidebar">
Supplementary information
</Section>
```

### 3. Specify Layout in Front-matter

Always specify which layout to use:

```markdown
---
title: My Page
layout: Markdn.Blazor.App.Components.Layout.MyLayout
---
```

### 4. Document Your Sections

Add comments in your layout to document available sections:

```razor
@inherits LayoutComponentBase

<div class="page">
    <!-- Available sections: PageHeader, Sidebar, CallToAction -->
    <header>
        <SectionOutlet SectionName="PageHeader" />
    </header>
    
    <main>@Body</main>
    
    <aside>
        <SectionOutlet SectionName="Sidebar" />
    </aside>
</div>
```

## Common Use Cases

### 1. Blog Post with Author Bio

```markdown
---
title: My Blog Post
layout: BlogLayout
---

<Section Name="AuthorBio">
## About the Author
John Doe is a software developer with 10 years of experience.
</Section>

# My Blog Post

Content of the blog post goes here...
```

### 2. Documentation Page with Table of Contents

```markdown
---
title: API Documentation
layout: DocsLayout
---

<Section Name="TableOfContents">
## Contents
- [Introduction](#introduction)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
</Section>

# Introduction

Documentation content...
```

### 3. Landing Page with Call-to-Action

```markdown
---
title: Product Landing Page
layout: LandingPageLayout
---

<Section Name="Hero">
# Amazing Product
Revolutionary solution for your business
</Section>

<Section Name="CallToAction">
[Get Started Now](/signup)
</Section>

## Features

Main content describing features...
```

## Limitations

### 1. Sections Must Be Top-Level

Nested sections are not supported:

```markdown
<!-- This will NOT work correctly -->
<Section Name="outer">
  <Section Name="inner">
    Content
  </Section>
</Section>
```

### 2. Section Tags in Code Blocks

While the parser correctly ignores section tags in fenced code blocks, be aware that:

- Backtick code blocks (```) are protected
- Inline code (`...`) is protected
- HTML comments are NOT protected (section tags in comments will be parsed)

### 3. Section Order in Output

Sections are always rendered before the main content in the generated Razor file. This matches Blazor's recommended pattern.

## Troubleshooting

### Problem: Layout Not Found

**Error**: `The type or namespace name 'MyLayout' could not be found`

**Solution**: Use the fully qualified layout name in front-matter:

```markdown
---
layout: Markdn.Blazor.App.Components.Layout.MyLayout
---
```

### Problem: Section Content Not Appearing

**Check**:
1. Verify the layout has a `<SectionOutlet SectionName="..." />` with matching name
2. Ensure the Section tags are properly closed
3. Check that section names match exactly (case-sensitive in the Name attribute)

### Problem: Section Tag Visible in Output

**Cause**: Section tag might be in a location where it wasn't parsed

**Solution**: Ensure proper tag syntax:
```markdown
<Section Name="test">
Content
</Section>
```

Not:
```markdown
<Section name="test">  <!-- lowercase 'name' won't work -->
<Section Name=test>    <!-- quotes required -->
```

## Migration from Static HTML Sections

If you were using regular HTML `<section>` tags, you can migrate to SectionContent:

**Before:**
```markdown
<section id="sidebar">
Content
</section>
```

**After:**
```markdown
<Section Name="Sidebar">
Content
</Section>
```

Note: Use capital 'S' to distinguish from HTML5 semantic `<section>` tags.

## Examples

See the example markdown file and layout:
- `/src/Markdn.Blazor.App/content/pages/SectionTest.md`
- `/src/Markdn.Blazor.App/Components/Layout/SectionTestLayout.razor`

## Technical Details

### How It Works

1. **Parsing**: The generator scans markdown for `<Section Name="...">...</Section>` tags
2. **Code Block Detection**: Identifies fenced code blocks and inline code to skip
3. **Content Extraction**: Extracts section content and processes it as markdown
4. **HTML Conversion**: Converts both section content and main content to HTML
5. **Razor Generation**: Creates `<SectionContent>` components for each section
6. **Order**: Sections are placed before main content in the generated file

### Performance

- Section parsing adds minimal overhead (~1-2ms per file)
- No runtime performance impact (sections are processed at build time)
- Generated Razor files are standard Blazor components

## See Also

- [Blazor Sections Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/sections)
- [Markdown to Razor Generator README](../tools/MarkdownToRazorGenerator/README.md)
- [Blazor Layouts](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/layouts)
