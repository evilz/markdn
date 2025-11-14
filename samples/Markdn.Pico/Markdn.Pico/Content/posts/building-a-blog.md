---
title: "Building a Blog with Markdn"
pubDate: 2025-11-05T14:30:00Z
description: "A step-by-step guide to building a blog using Markdn content collections."
author: "Jane Smith"
tags: ["tutorial", "blog", "content-collections"]
---

# Building a Blog with Markdn

In this tutorial, we'll build a complete blog using Markdn's content collections feature.

## Step 1: Define Your Collection

Start by creating a `collections.json` file that defines the schema for your blog posts:

```json
{
  "collections": {
    "posts": {
      "folder": "Content/posts",
      "schema": {
        "properties": {
          "title": { "type": "string" },
          "pubDate": { "type": "string", "format": "date-time" },
          "description": { "type": "string" }
        },
        "required": ["title", "pubDate"]
      }
    }
  }
}
```

## Step 2: Create Content

Add markdown files to your `Content/posts` directory with frontmatter:

```markdown
---
title: "My First Post"
pubDate: 2025-11-01T10:00:00Z
description: "This is my first blog post!"
---

# Hello World

Welcome to my blog!
```

## Step 3: Query Your Collection

Use the generated content service to access your posts with full type safety:

```csharp
var posts = contentService.GetCollection();
foreach (var post in posts)
{
    Console.WriteLine($"{post.Title} - {post.PubDate}");
}
```

## Conclusion

Markdn makes it easy to build content-driven applications with type safety and great developer experience!
