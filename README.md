# Markdn

A lightweight, file-based headless CMS for serving Markdown content via REST API. Built with ASP.NET Core 8.0 for high performance and simplicity.

## Features

- 📝 **Markdown-First**: Store content as simple `.md` files with YAML front-matter
- 🚀 **High Performance**: Efficient file system scanning with configurable caching
- 🔍 **Advanced Filtering**: Filter by tags, categories, and date ranges
- 📊 **Flexible Sorting**: Sort by date, title, or last modified (ascending/descending)
- 🔒 **Secure by Default**: Path traversal prevention, input validation, security headers
- 📄 **Pagination**: Built-in pagination for large content collections
- 🎯 **RESTful API**: Clean, predictable endpoints following REST best practices
- 📦 **Zero Database**: No database required - just files
- ⚡ **GitHub Flavored Markdown**: Full GFM support with tables, task lists, strikethrough, and more

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
