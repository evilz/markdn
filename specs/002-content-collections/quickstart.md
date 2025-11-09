# Quickstart: Content Collections

**Feature**: Content Collections  
**Audience**: Developers integrating Content Collections into Markdn  
**Time to Complete**: 15 minutes

## Overview

Content Collections provides type-safe, schema-validated content management for Markdown and JSON files. This guide walks you through defining a collection, adding content, and querying it via the API.

---

## Prerequisites

- .NET 8.0 SDK installed
- Markdn API project cloned and building
- Basic understanding of JSON Schema
- REST client (curl, Postman, or similar)

---

## Step 1: Define a Collection Schema

Create a `collections.json` file in your content root directory:

```json
{
  "contentRootPath": "content",
  "collections": {
    "blog": {
      "folder": "blog",
      "schema": {
        "type": "object",
        "properties": {
          "title": {
            "type": "string",
            "minLength": 1,
            "description": "Post title"
          },
          "author": {
            "type": "string",
            "description": "Author name"
          },
          "publishDate": {
            "type": "string",
            "format": "date",
            "description": "Publication date (YYYY-MM-DD)"
          },
          "tags": {
            "type": "array",
            "items": {
              "type": "string"
            },
            "description": "Topic tags"
          },
          "draft": {
            "type": "boolean",
            "description": "Draft status"
          }
        },
        "required": ["title", "publishDate"]
      }
    }
  }
}
```

**Key Points**:
- `contentRootPath`: Base directory for all collections
- `folder`: Relative path for this collection's content files
- `schema`: JSON Schema (Draft 7) defining required fields and types
- `required`: Fields that must be present in every content file

---

## Step 2: Add Content Files

Create Markdown files in `content/blog/`:

### File: `content/blog/getting-started.md`

```markdown
---
title: Getting Started with Markdn
author: Jane Doe
publishDate: 2025-11-09
tags:
  - tutorial
  - beginner
draft: false
---

# Getting Started with Markdn

Welcome to Markdn! This guide will help you...
```

### File: `content/blog/advanced-tips.md`

```markdown
---
title: Advanced Markdn Tips
author: John Smith
publishDate: 2025-11-10
tags:
  - advanced
  - tips
draft: false
---

# Advanced Tips

Here are some advanced techniques...
```

**Validation**:
- ✅ Both files have required fields (`title`, `publishDate`)
- ✅ Field types match schema (strings, date, array, boolean)
- ✅ Slugs will be auto-generated from filenames

---

## Step 3: Start the Application

Run the Markdn API:

```bash
cd src/Markdn.Api
dotnet run
```

**What Happens at Startup**:
1. Loads `collections.json`
2. Discovers content files in `content/blog/`
3. Validates all files against schema
4. Caches validated content items
5. Logs validation summary

**Expected Log Output**:
```
[Info] Loading collection definitions from collections.json
[Info] Found 1 collection(s): blog
[Info] Validating collection 'blog'...
[Info] Found 2 content file(s) in content/blog
[Info] Validated 2 items in 45ms (2 valid, 0 invalid)
[Info] Collection 'blog' ready to serve
```

---

## Step 4: Query Collections

### List All Collections

```bash
curl http://localhost:5000/api/collections
```

**Response**:
```json
{
  "collections": [
    {
      "name": "blog",
      "folder": "content/blog",
      "itemCount": 2,
      "schema": {
        "type": "object",
        "properties": { ... },
        "required": ["title", "publishDate"]
      }
    }
  ]
}
```

---

### Get All Items in a Collection

```bash
curl http://localhost:5000/api/collections/blog/items
```

**Response**:
```json
{
  "items": [
    {
      "id": "getting-started",
      "collectionName": "blog",
      "slug": "getting-started",
      "frontMatter": {
        "title": "Getting Started with Markdn",
        "author": "Jane Doe",
        "publishDate": "2025-11-09",
        "tags": ["tutorial", "beginner"],
        "draft": false
      },
      "content": "# Getting Started with Markdn\n\nWelcome...",
      "lastModified": "2025-11-09T10:00:00Z"
    },
    {
      "id": "advanced-tips",
      "collectionName": "blog",
      "slug": "advanced-tips",
      "frontMatter": {
        "title": "Advanced Markdn Tips",
        "author": "John Smith",
        "publishDate": "2025-11-10",
        "tags": ["advanced", "tips"],
        "draft": false
      },
      "content": "# Advanced Tips\n\nHere are...",
      "lastModified": "2025-11-09T11:00:00Z"
    }
  ],
  "count": 2,
  "total": 2
}
```

---

### Get a Single Item

```bash
curl http://localhost:5000/api/collections/blog/items/getting-started
```

**Response**:
```json
{
  "id": "getting-started",
  "collectionName": "blog",
  "slug": "getting-started",
  "frontMatter": {
    "title": "Getting Started with Markdn",
    "author": "Jane Doe",
    "publishDate": "2025-11-09",
    "tags": ["tutorial", "beginner"],
    "draft": false
  },
  "content": "# Getting Started with Markdn\n\nWelcome...",
  "lastModified": "2025-11-09T10:00:00Z"
}
```

---

## Step 5: Advanced Querying

### Filter by Author

```bash
curl "http://localhost:5000/api/collections/blog/items?\$filter=author%20eq%20'Jane%20Doe'"
```

**Query**: `$filter=author eq 'Jane Doe'`

**Response**: Only posts by Jane Doe

---

### Filter by Date Range

```bash
curl "http://localhost:5000/api/collections/blog/items?\$filter=publishDate%20gt%20'2025-11-09'"
```

**Query**: `$filter=publishDate gt '2025-11-09'`

**Response**: Posts published after November 9, 2025

---

### Sort by Date (Descending)

```bash
curl "http://localhost:5000/api/collections/blog/items?\$orderby=publishDate%20desc"
```

**Query**: `$orderby=publishDate desc`

**Response**: Posts sorted newest first

---

### Pagination

```bash
curl "http://localhost:5000/api/collections/blog/items?\$top=1&\$skip=0"
```

**Query**: `$top=1&$skip=0`

**Response**: First post only (page 1)

---

### Field Selection

```bash
curl "http://localhost:5000/api/collections/blog/items?\$select=title,author,publishDate"
```

**Query**: `$select=title,author,publishDate`

**Response**: Only specified fields returned (excludes `content`, `tags`, etc.)

---

### Combined Query

```bash
curl "http://localhost:5000/api/collections/blog/items?\$filter=draft%20eq%20false%20and%20publishDate%20gt%20'2025-11-01'&\$orderby=publishDate%20desc&\$top=10&\$select=title,author,publishDate"
```

**Query**:
- Filter: `draft eq false and publishDate gt '2025-11-01'`
- Sort: `publishDate desc`
- Pagination: `$top=10`
- Fields: `title,author,publishDate`

**Response**: Published posts from November 2025 onwards, sorted newest first, limited to 10 items, with selected fields only

---

## Step 6: Handle Validation Errors

Create an invalid content file to test validation:

### File: `content/blog/broken-post.md`

```markdown
---
author: Test Author
tags:
  - test
---

# This post is missing required fields!
```

**Restart the application** and check logs:

```
[Warning] Validation failed for content/blog/broken-post.md
[Warning]   - RequiredFieldMissing: Field 'title' is required but missing
[Warning]   - RequiredFieldMissing: Field 'publishDate' is required but missing
[Info] Validated 3 items in 52ms (2 valid, 1 invalid)
```

The invalid file is **not served** via the API.

---

### Check Validation Status

```bash
curl http://localhost:5000/api/collections/blog/validate
```

**Response**:
```json
{
  "collectionName": "blog",
  "totalItems": 3,
  "validItems": 2,
  "invalidItems": 1,
  "errors": [
    {
      "itemId": "broken-post",
      "fieldName": "title",
      "errorType": "RequiredFieldMissing",
      "message": "Required field 'title' is missing"
    },
    {
      "itemId": "broken-post",
      "fieldName": "publishDate",
      "errorType": "RequiredFieldMissing",
      "message": "Required field 'publishDate' is missing"
    }
  ],
  "warnings": [],
  "lastValidatedAt": "2025-11-09T12:00:00Z"
}
```

---

## Step 7: Runtime Content Changes

Content Collections watches for file changes at runtime.

**Edit an existing file** (e.g., change the title in `getting-started.md`):

```markdown
---
title: Getting Started with Markdn (Updated!)
author: Jane Doe
publishDate: 2025-11-09
tags:
  - tutorial
  - beginner
draft: false
---
```

**Save the file**. The system will:
1. Detect file change via FileSystemWatcher
2. Re-validate the file
3. Invalidate cache
4. Next API request returns updated content

**Check logs**:
```
[Info] File changed: content/blog/getting-started.md
[Info] Re-validating content/blog/getting-started.md
[Info] Validation passed (45ms)
[Info] Cache invalidated for blog:getting-started
```

---

## Step 8: Add Extra Fields (Warnings)

Content Collections allows extra fields not in the schema but logs warnings.

### File: `content/blog/future-post.md`

```markdown
---
title: Future Features
author: Jane Doe
publishDate: 2025-12-01
tags:
  - roadmap
draft: false
featured: true
estimatedReadTime: 5
---

# Future Features

Coming soon...
```

**Note**: `featured` and `estimatedReadTime` are **not in the schema**.

**Logs**:
```
[Warning] Extra field 'featured' in blog:future-post (not in schema)
[Warning] Extra field 'estimatedReadTime' in blog:future-post (not in schema)
[Info] Validated 1 items in 12ms (1 valid, 0 invalid, 2 warnings)
```

**API Response**: Extra fields are **preserved** in `frontMatter`:

```json
{
  "id": "future-post",
  "frontMatter": {
    "title": "Future Features",
    "author": "Jane Doe",
    "publishDate": "2025-12-01",
    "tags": ["roadmap"],
    "draft": false,
    "featured": true,
    "estimatedReadTime": 5
  },
  ...
}
```

---

## Common Patterns

### Pattern 1: Blog with Categories

```json
{
  "blog": {
    "folder": "blog",
    "schema": {
      "properties": {
        "title": { "type": "string" },
        "category": {
          "type": "string",
          "enum": ["tutorial", "news", "release"]
        },
        "publishDate": { "type": "string", "format": "date" }
      },
      "required": ["title", "category", "publishDate"]
    }
  }
}
```

**Query**: `$filter=category eq 'tutorial'`

---

### Pattern 2: Documentation with Ordering

```json
{
  "docs": {
    "folder": "docs",
    "schema": {
      "properties": {
        "title": { "type": "string" },
        "order": { "type": "number" },
        "section": { "type": "string" }
      },
      "required": ["title", "order"]
    }
  }
}
```

**Query**: `$orderby=order asc`

---

### Pattern 3: Product Catalog

```json
{
  "products": {
    "folder": "products",
    "schema": {
      "properties": {
        "name": { "type": "string" },
        "price": { "type": "number", "minimum": 0 },
        "sku": { "type": "string", "pattern": "^[A-Z]{3}-[0-9]{4}$" },
        "inStock": { "type": "boolean" }
      },
      "required": ["name", "price", "sku"]
    }
  }
}
```

**Query**: `$filter=inStock eq true and price lt 100&$orderby=price asc`

---

## Troubleshooting

### Issue: Collection not found

**Error**: `Collection 'blog' does not exist`

**Solution**:
- Check `collections.json` exists in content root
- Verify collection name matches exactly (case-sensitive)
- Check logs for configuration loading errors

---

### Issue: Validation always fails

**Error**: `Required field 'title' is missing` (but title is present)

**Solution**:
- Check YAML front-matter syntax (indentation matters)
- Ensure field names match exactly (case-sensitive)
- Verify date format is ISO 8601 (YYYY-MM-DD)

---

### Issue: Query returns no results

**Error**: Empty `items` array despite content existing

**Solution**:
- Check filter syntax (use single quotes for strings: `author eq 'John'`)
- URL-encode special characters (spaces, single quotes)
- Verify field names exist in schema
- Check logs for query parsing errors

---

### Issue: Content changes not reflected

**Error**: API returns stale content after file edit

**Solution**:
- Wait ~300ms for file watcher debouncing
- Check logs for file change detection
- Verify file watcher errors (permissions, network shares)
- Restart application if file watcher failed

---

## Next Steps

- **Advanced Schemas**: Explore nested objects, pattern validation, enum constraints
- **Custom Slugs**: Use `slug` field in front-matter for explicit URL identifiers
- **Multiple Collections**: Define docs, blog, products, etc.
- **API Integration**: Build frontends consuming the Collections API
- **Performance Tuning**: Monitor query performance, adjust caching strategies

---

## Summary

You've successfully:
✅ Defined a collection schema  
✅ Added validated content files  
✅ Queried content via REST API  
✅ Filtered, sorted, and paginated results  
✅ Handled validation errors  
✅ Observed runtime content updates  

**Full API Reference**: See [contracts/openapi.yaml](./contracts/openapi.yaml)  
**Data Model**: See [data-model.md](./data-model.md)
