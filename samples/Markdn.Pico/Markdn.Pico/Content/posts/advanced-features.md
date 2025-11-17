---
title: "Advanced Markdn Features"
pubDate: 2025-11-10T09:15:00Z
description: "Explore advanced features of Markdn including custom components and dynamic routing."
author: "John Doe"
tags: ["advanced", "components", "routing"]
componentNamespaces:
    - Markdn.Pico.Components
---

# Advanced Markdn Features

Once you're comfortable with the basics, it's time to explore some of Markdn's more advanced features.

## Custom Blazor Components in Markdown

You can embed custom Blazor components directly in your Markdown:


<Heading Title="coucou" />


## Dynamic Routing

Markdn automatically generates routes for your content based on the file structure and frontmatter.

## Type-Safe Frontmatter

With Markdn's source generator, you get compile-time type safety for all your frontmatter properties:

```csharp
// This is type-safe!
string title = post.Title;
DateTime pubDate = post.PubDate;
List<string> tags = post.Tags;
```

## Query and Filter

Use LINQ to query your content collections:

```csharp
var recentPosts = contentService
    .GetCollection()
    .Where(p => p.PubDate > DateTime.Now.AddMonths(-1))
    .OrderByDescending(p => p.PubDate)
    .Take(5);
```

## What's Next?

Check out the documentation for more advanced use cases and best practices!
