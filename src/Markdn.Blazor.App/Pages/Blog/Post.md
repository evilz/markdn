---
title: "Sample Blog Post"
date: 2025-11-11
url: /blog/sample-post
componentNamespaces:
  - Markdn.Blazor.App.Components
  - Markdn.Blazor.App.Components.Pages
---

# Sample Blog Post

This is a comprehensive example demonstrating many generator features:

- Title from front matter
- Uses a built-in component reference: <Counter />
- Includes inline expression: Current year is @DateTime.Now.Year

@code {
    private string Author => "Example Author";
}

Enjoy the generated component!
