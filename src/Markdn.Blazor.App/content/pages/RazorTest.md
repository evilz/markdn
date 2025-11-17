---
title: Razor Syntax Test
namespace: Markdn.Blazor.App.Pages
#componentNamespaces:
#    - Markdn.Blazor.App.Components
#    - Markdn.Blazor.App.Components.Shared
#    - Markdn.Blazor.App.Components.Pages
---

# Razor Syntax Preservation Test

This page demonstrates Razor syntax preservation through Markdown processing.

## Code Block Test


## Expression Test

Current time: @DateTime.Now


## Component Test

<Counter />

## Mixed Content

You can mix **Markdown** with Razor expressions seamlessly.

- Another item with @DateTime.Now.ToString()
