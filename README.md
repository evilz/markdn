# Markdn

A lightweight, file-based headless CMS for serving Markdown content via REST API. Built with ASP.NET Core 8.0 for high performance and simplicity.

## Features

- 📝 **Markdown-First**: Store content as simple `.md` files with YAML front-matter
- 🚀 **High Performance**: Efficient file system scanning with configurable caching
- 🔍 **Advanced Filtering**: Filter by tags, categories, and date ranges with OData-style queries
- 📊 **Flexible Sorting**: Sort by date, title, or last modified (ascending/descending)
- 🔒 **Secure by Default**: Path traversal prevention, input validation, security headers
- 📄 **Pagination**: Built-in pagination for large content collections
- 🎯 **RESTful API**: Clean, predictable endpoints following REST best practices
- 📦 **Zero Database**: No database required - just files
- ⚡ **GitHub Flavored Markdown**: Full GFM support with tables, task lists, strikethrough, and more
- 🏗️ **Type-Safe Collections**: Define schemas for content validation and type safety (like Astro Content Collections)
- ✅ **Automatic Validation**: Content validated at startup and runtime against collection schemas
- 🔄 **Runtime Monitoring**: File watcher integration for detecting content and schema changes
- 🔎 **Advanced Queries**: OData-like query syntax with `$filter`, `$orderby`, `$top`, `$skip`
- 🎨 **Source Generator**: Compile-time source generator for Blazor apps with typed `GetCollection()` and `GetEntry()` API

## Content Collections

Markdn supports **Content Collections** in two flavors:

1. **Runtime Collections API** - REST API for serving validated content (documented below)
2. **Source-Generated Collections** - Compile-time source generator for Blazor apps with type-safe access ([see documentation](docs/source-generated-collections.md))

Both are inspired by [Astro Content Collections](https://docs.astro.build/en/guides/content-collections/) and provide type-safe, schema-validated content management.

### What are Content Collections?

Content Collections allow you to:

- **Define schemas** for your content types (blog posts, documentation pages, products, etc.)
- **Validate content** automatically against these schemas
- **Query content** with type safety and advanced filtering
- **Organize content** in logical folders with consistent structure

### Quick Example

1. **Define a collection** in `collections.json`:

```json
{
  "blog": {
    "type": "content",
    "schema": {
      "type": "object",
      "properties": {
        "title": { "type": "string" },
        "author": { "type": "string" },
        "date": { "type": "string", "format": "date" },
        "tags": { "type": "array", "items": { "type": "string" } }
      },
      "required": ["title", "author", "date"]
    }
  }
}
```

2. **Create content** in `content/blog/my-post.md`:

```markdown
---
title: Getting Started with Collections
author: Jane Doe
date: 2024-01-15
tags: [tutorial, getting-started]
---

# Getting Started

Your content here...
```

3. **Query the collection**:

```bash
# Get all blog posts
curl http://localhost:5219/api/collections/blog/items

# Filter by author
curl "http://localhost:5219/api/collections/blog/items?$filter=author eq 'Jane Doe'"

# Sort and paginate
curl "http://localhost:5219/api/collections/blog/items?$orderby=date desc&$top=10"
```

### Collection Schema Format

Collections are defined in a `collections.json` file at the root of your content directory:

```json
{
  "contentRootPath": "content",
  "collections": {
    "collection-name": {
      "folder": "collection-folder",
      "schema": {
        "type": "object",
        "properties": {
          "field-name": {
            "type": "string",
            "format": "date",
            "description": "Field description"
          },
          "multi-type-field": {
            "type": ["string", "number"],
            "description": "Field that accepts multiple types"
          }
        },
        "required": ["field-name"]
      }
    }
  }
}
```

**Supported field types:**
- `string` - Text fields (with optional formats: `date`, `date-time`, `email`, `uri`, etc.)
- `number` - Decimal numbers
- `integer` - Whole numbers
- `boolean` - True/false values
- `array` - Lists of items
- `object` - Nested structures
- `["type1", "type2"]` - Multiple types (JSON Schema array syntax)

### Validation

Content is validated automatically:

- **At startup**: Eager validation of all content files (completes in <5s for 1000 items)
- **At runtime**: Lazy validation when content is accessed
- **On file changes**: Content is revalidated when files are modified
- **On schema changes**: All content is revalidated when `collections.json` changes

**Validation errors** are logged with details:
- Missing required fields
- Type mismatches
- Invalid formats
- Extra fields (logged as warnings, content is preserved)

**Validation endpoint**:
```bash
# Validate entire collection
curl -X POST http://localhost:5219/api/collections/blog/validate-all
```

Response includes validation statistics and detailed error messages.

### Querying Collections

Collections support OData-style query syntax for powerful filtering and sorting:

#### Filter Operations

```bash
# Equality
$filter=author eq 'Jane Doe'

# Comparison
$filter=views gt 1000
$filter=date ge '2024-01-01'

# Logical operators
$filter=author eq 'Jane Doe' and category eq 'Tutorial'
$filter=views gt 1000 or featured eq true

# String functions
$filter=contains(title, 'Getting Started')
$filter=startswith(title, 'How to')
```

**Supported operators:**
- `eq` (equal), `ne` (not equal)
- `gt` (greater than), `ge` (greater than or equal)
- `lt` (less than), `le` (less than or equal)
- `and`, `or`, `not`
- `contains`, `startswith`, `endswith`

#### Sorting

```bash
# Single field
$orderby=date desc

# Multiple fields
$orderby=category asc, date desc
```

#### Pagination

```bash
# Top N items
$top=10

# Skip N items
$skip=20

# Combine for pagination
$top=10&$skip=20  # Page 3 (items 21-30)
```

#### Complete Example

```bash
# Get top 10 featured blog posts by Jane Doe,
# sorted by views (descending)
curl "http://localhost:5219/api/collections/blog/items?\
$filter=author eq 'Jane Doe' and featured eq true&\
$orderby=views desc&\
$top=10"
```

### Collections API Reference

#### `GET /api/collections`

List all available collections.

**Response:** `200 OK` with collection names and schemas

#### `GET /api/collections/{name}`

Get metadata for a specific collection.

**Path Parameters:**
- `name` (string, required): Collection name

**Response:** 
- `200 OK` with collection metadata
- `404 Not Found` if collection doesn't exist

#### `GET /api/collections/{name}/items`

Get all items from a collection.

**Path Parameters:**
- `name` (string, required): Collection name

**Query Parameters:**
- `$filter` (string, optional): OData-style filter expression
- `$orderby` (string, optional): Sort expression (field [asc|desc])
- `$top` (integer, optional): Maximum items to return
- `$skip` (integer, optional): Number of items to skip

**Response:**
- `200 OK` with array of content items
- `400 Bad Request` if query syntax is invalid
- `404 Not Found` if collection doesn't exist

#### `GET /api/collections/{name}/items/{id}`

Get a single item from a collection by slug.

**Path Parameters:**
- `name` (string, required): Collection name
- `id` (string, required): Item slug

**Response:**
- `200 OK` with content item
- `404 Not Found` if collection or item doesn't exist

#### `POST /api/collections/{name}/validate-all`

Manually trigger validation for all items in a collection.

**Path Parameters:**
- `name` (string, required): Collection name

**Response:**
- `200 OK` with validation report (total items, valid items, invalid items, error details)
- `404 Not Found` if collection doesn't exist

### Performance

Collections are optimized for performance:

- **Caching**: Validated content and query results are cached in memory
- **Cache Invalidation**: Automatic cache invalidation on file and schema changes
- **Streaming**: Large collections use streaming to avoid memory issues
- **Metrics**: Built-in metrics for cache hit rates and query performance
- **Goal**: <200ms p95 latency for queries with filters/sorting on 1000 items

### Monitoring

Collections include observability features:

- **Health Check**: `GET /api/health` includes collection validation status
- **Distributed Tracing**: OpenTelemetry ActivitySource for operation tracking
- **Metrics**: Custom metrics for cache hit/miss rates
- **Structured Logging**: Detailed logs for validation, query operations, and file changes

### Migration from Basic Content

Existing content works without changes! Collections are optional:

1. Start with basic content API (`/api/content`)
2. Add `collections.json` when you need schema validation
3. Use Collections API (`/api/collections`) for type-safe queries
4. Both APIs coexist - use whichever fits your needs

### Source-Generated Collections for Blazor

For **Blazor applications**, use the source generator to get compile-time type safety and IntelliSense:

```csharp
// Generated at compile-time from collections.json
var postsService = new PostsService();
var posts = postsService.GetCollection(); // Type: List<PostsEntry>
var post = postsService.GetEntry("my-slug"); // Type: PostsEntry?

// Type-safe property access with IntelliSense
string title = post.Title;
DateTime pubDate = post.PubDate;
List<string> tags = post.Tags;
```

**Key benefits:**
- ✅ Compile-time type safety
- ✅ IntelliSense for all frontmatter properties  
- ✅ No runtime overhead for type generation
- ✅ Astro-like `GetCollection()` and `GetEntry()` API

**📖 [Complete Source Generator Documentation](docs/source-generated-collections.md)**



## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- A text editor or IDE (VS Code, Visual Studio, Rider, etc.)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/markdn.git
cd markdn
```

2. Create a content directory and add some Markdown files:
```bash
mkdir -p content
```

3. Create a sample Markdown file `content/hello-world.md`:
```markdown
---
title: Hello World
date: 2024-01-15
author: John Doe
tags: [welcome, introduction]
category: Getting Started
description: Welcome to Markdn!
---

# Hello World

Welcome to **Markdn**, a lightweight headless CMS for Markdown content!

## Features

- Simple file-based storage
- Full GitHub Flavored Markdown support
- REST API for easy integration
```

4. Configure the application (optional - defaults work out of the box):

Edit `src/Markdn.Api/appsettings.json`:
```json
{
  "Markdn": {
    "ContentDirectory": "content",
    "MaxFileSizeBytes": 5242880,
    "DefaultPageSize": 50
  }
}
```

5. Run the application:
```bash
dotnet run --project src/Markdn.Api
```

The API will be available at `http://localhost:5219` (or the port shown in the console).

### Using the API

#### Get All Content

```bash
curl http://localhost:5219/api/content
```

Response:
```json
{
  "items": [
    {
      "slug": "2024-01-15-hello-world",
      "title": "Hello World",
      "date": "2024-01-15T00:00:00",
      "author": "John Doe",
      "category": "Getting Started",
      "description": "Welcome to Markdn!",
      "tags": ["welcome", "introduction"]
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Get Content by Slug

```bash
curl http://localhost:5219/api/content/2024-01-15-hello-world
```

Response includes the full HTML-rendered content.

#### Filter Content

Filter by tag:
```bash
curl "http://localhost:5219/api/content?tag=welcome"
```

Filter by category:
```bash
curl "http://localhost:5219/api/content?category=Getting%20Started"
```

Filter by date range:
```bash
curl "http://localhost:5219/api/content?dateFrom=2024-01-01&dateTo=2024-12-31"
```

Combine filters:
```bash
curl "http://localhost:5219/api/content?tag=welcome&category=Getting%20Started&sortBy=date&sortOrder=desc"
```

#### Pagination

```bash
curl "http://localhost:5219/api/content?page=1&pageSize=10"
```

#### Sorting

Sort by date (default):
```bash
curl "http://localhost:5219/api/content?sortBy=date&sortOrder=desc"
```

Sort by title:
```bash
curl "http://localhost:5219/api/content?sortBy=title&sortOrder=asc"
```

Sort by last modified:
```bash
curl "http://localhost:5219/api/content?sortBy=lastmodified&sortOrder=desc"
```

## Configuration

### appsettings.json

| Setting | Description | Default |
|---------|-------------|---------|
| `ContentDirectory` | Path to directory containing Markdown files (relative or absolute) | `content` |
| `MaxFileSizeBytes` | Maximum file size to process (prevents memory issues) | `5242880` (5 MB) |
| `DefaultPageSize` | Default number of items per page | `50` |

### Environment Variables

Override configuration using environment variables:
```bash
export Markdn__ContentDirectory="/path/to/content"
export Markdn__MaxFileSizeBytes=10485760
export Markdn__DefaultPageSize=25
```

## Content Format

### Front-Matter

Markdn uses YAML front-matter for metadata:

```markdown
---
title: My Blog Post          # Required for good SEO
slug: custom-slug            # Optional: override auto-generated slug
date: 2024-01-15             # Optional: defaults to file creation date
author: Jane Smith           # Optional
tags: [tech, tutorial]       # Optional: array of tags
category: Tutorial           # Optional: single category
description: A brief summary # Optional: used in listings
custom_field: value          # Optional: any additional fields
---

# Your Content Here

Markdown content goes here...
```

### Slug Generation

Slugs are generated automatically using this precedence:
1. Explicit `slug` in front-matter
2. Date prefix + filename (if filename starts with YYYY-MM-DD)
3. Sanitized filename

Examples:
- `2024-01-15-hello-world.md` → `2024-01-15-hello-world`
- `hello-world.md` → `hello-world`
- Front-matter with `slug: custom` → `custom`

### Supported Markdown

Markdn uses [Markdig](https://github.com/xoofx/markdig) with GitHub Flavored Markdown extensions:

- **Headings**: `# H1` through `###### H6`
- **Emphasis**: `*italic*`, `**bold**`, `~~strikethrough~~`
- **Lists**: Ordered and unordered
- **Links**: `[text](url)`, autolinks
- **Images**: `![alt](url)`
- **Code**: Inline `` `code` `` and fenced code blocks with syntax highlighting
- **Tables**: GitHub-style tables
- **Task Lists**: `- [ ]` and `- [x]`
- **Blockquotes**: `> quote`
- **Horizontal Rules**: `---`
- **HTML**: Inline HTML is supported

## API Reference

### Endpoints

#### `GET /api/content`

Get a paginated list of all content items.

**Query Parameters:**
- `tag` (string, optional): Filter by tag (case-insensitive, partial match)
- `category` (string, optional): Filter by category (case-insensitive, exact match)
- `dateFrom` (string, optional): Filter items with date >= this value (ISO 8601 format)
- `dateTo` (string, optional): Filter items with date <= this value (ISO 8601 format)
- `page` (integer, optional, default: 1): Page number (1-indexed)
- `pageSize` (integer, optional, default: 50, max: 100): Items per page
- `sortBy` (string, optional, default: "date"): Sort field - `date`, `title`, or `lastmodified`
- `sortOrder` (string, optional, default: "desc"): Sort direction - `asc` or `desc`

**Response:** `200 OK` with `ContentListResponse`

#### `GET /api/content/{slug}`

Get a single content item by slug.

**Path Parameters:**
- `slug` (string, required): The slug of the content item

**Response:** 
- `200 OK` with `ContentItemResponse` if found
- `404 Not Found` if slug doesn't exist

#### `GET /api/health`

Health check endpoint for monitoring.

**Response:** `200 OK` with health status

### Response Models

See [docs/api.md](docs/api.md) for detailed schema documentation.

## Security

Markdn includes multiple security measures:

- **Path Traversal Prevention**: All file paths are validated to stay within the content directory
- **Input Validation**: Slug validation (max 200 chars, alphanumeric + hyphens/underscores only)
- **Query Parameter Sanitization**: Tag/category length limits, null byte rejection
- **Security Headers**: 
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Content-Security-Policy: default-src 'self'`
- **File Size Limits**: Configurable max file size (default 5 MB)

## Development

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Run in Development Mode

```bash
dotnet run --project src/Markdn.Api --environment Development
```

Development mode includes:
- Swagger UI at `/swagger`
- Detailed error messages
- Development exception page

## Deployment

See [docs/deployment.md](docs/deployment.md) for Docker, Kubernetes, and cloud deployment guides.

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## Roadmap

- [ ] File system watching for live content updates
- [ ] Multiple rendering formats (JSON, HTML, plain text)
- [ ] Content caching with automatic invalidation
- [ ] Search functionality (full-text)
- [ ] Media asset management
- [ ] Multi-language support
- [ ] Content versioning
- [ ] Draft/published workflow

## Support

- 📖 [Documentation](docs/)
- 🐛 [Issue Tracker](https://github.com/yourusername/markdn/issues)
- 💬 [Discussions](https://github.com/yourusername/markdn/discussions)
