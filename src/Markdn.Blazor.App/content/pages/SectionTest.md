---
title: Section Outlet Test Page
slug: section-test
route: /section-test
layout: Markdn.Blazor.App.Components.Layout.SectionTestLayout
---

<Section Name="PageHeader">
# Welcome to Section Test!
This content will be rendered in the PageHeader section of the layout.
</Section>

<Section Name="Sidebar">
## Quick Links
- [Home](/)
- [About](/about)
- [Contact](/contact)

### Features
This sidebar section demonstrates how you can use **markdown** formatting inside sections!
</Section>

# Main Content Area

This is the main content of the page. It appears in the Body section of the layout.

## Features of Section Support

1. **Define sections using HTML-like syntax**: Use Section tags in your markdown
2. **Full markdown support**: All markdown features work inside sections
3. **Clean separation**: Main content stays separate from section content
4. **Blazor integration**: Sections are converted to SectionContent components

## Example Usage

Here's how you use sections in markdown - define a section with a name:

```text
<Section Name="MySectionName">
  Your markdown content here
  Can include any markdown formatting!
</Section>
```

And in your layout Razor file, use:

```text
<SectionOutlet SectionName="MySectionName" />
```

## Benefits

- Keep your content organized
- Use different layouts with different sections
- Maintain clean separation of concerns
- Full markdown support everywhere
- No conflicts with standard markdown syntax

