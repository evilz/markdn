---
url:
  - /
  - /home
  - /index
title: Home Page
---

# Welcome Home!

This is the **Home** page with multiple routes demonstrating array URL support.

## Multi-Route Features

This page is accessible via **three** different URLs:
- `/` (root)
- `/home`
- `/index`

All three routes point to the same component, with auto-generated `[Route(...)]` attributes for each.

## How It Works

The YAML front matter specifies:
```yaml
url:
  - /
  - /home
  - /index
```

The source generator emits three `[Route(...)]` attributes on the component class.
