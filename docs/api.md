# Markdn API Documentation

Complete REST API reference for Markdn headless CMS.

## Base URL

```
http://localhost:5219/api
```

In production, replace with your actual domain.

## Authentication

Currently, Markdn does not require authentication for read operations. This may change in future versions for write operations.

## Endpoints

### Get All Content

Retrieve a paginated list of content items with optional filtering and sorting.

```http
GET /api/content
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `tag` | string | No | - | Filter by tag (case-insensitive, partial match). Example: `tag=tutorial` |
| `category` | string | No | - | Filter by category (case-insensitive, exact match). Example: `category=Blog` |
| `dateFrom` | string | No | - | Filter items with date >= this value. ISO 8601 format. Example: `dateFrom=2024-01-01` |
| `dateTo` | string | No | - | Filter items with date <= this value. ISO 8601 format. Example: `dateTo=2024-12-31` |
| `page` | integer | No | 1 | Page number (1-indexed). Must be >= 1. |
| `pageSize` | integer | No | 50 | Items per page. Must be between 1 and 100. |
| `sortBy` | string | No | date | Sort field. Allowed values: `date`, `title`, `lastmodified` |
| `sortOrder` | string | No | desc | Sort direction. Allowed values: `asc`, `desc` |

#### Validation Rules

- `tag`: Max 100 characters, no null bytes
- `category`: Max 100 characters, no null bytes
- `dateFrom`/`dateTo`: Must be valid date strings parseable by .NET
- `page`: Must be >= 1
- `pageSize`: Must be >= 1 and <= 100
- `sortBy`: Must be one of: `date`, `title`, `lastmodified`
- `sortOrder`: Must be one of: `asc`, `desc`

#### Response

**Status Code:** `200 OK`

**Content-Type:** `application/json`

**Body:**

```json
{
  "items": [
    {
      "slug": "2024-01-15-hello-world",
      "title": "Hello World",
      "date": "2024-01-15T00:00:00",
      "author": "John Doe",
      "category": "Tutorial",
      "description": "A beginner's guide to Markdn",
      "tags": ["tutorial", "getting-started"]
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

#### Error Responses

**Status Code:** `400 Bad Request`

Returned when:
- Invalid `page` or `pageSize` values
- Invalid `sortBy` or `sortOrder` values
- Invalid `tag` or `category` format

**Example:**

```json
{
  "error": "Bad Request"
}
```

**Status Code:** `500 Internal Server Error`

Returned when an unexpected error occurs. Check server logs for details.

#### Examples

**Get first page of all content:**

```bash
curl http://localhost:5219/api/content
```

**Filter by tag:**

```bash
curl "http://localhost:5219/api/content?tag=tutorial"
```

**Filter by category and sort by title:**

```bash
curl "http://localhost:5219/api/content?category=Blog&sortBy=title&sortOrder=asc"
```

**Date range with pagination:**

```bash
curl "http://localhost:5219/api/content?dateFrom=2024-01-01&dateTo=2024-06-30&page=2&pageSize=20"
```

**Combine multiple filters:**

```bash
curl "http://localhost:5219/api/content?tag=tech&category=Tutorial&dateFrom=2024-01-01&sortBy=date&sortOrder=desc&page=1&pageSize=10"
```

---

### Get Content by Slug

Retrieve a single content item by its unique slug.

```http
GET /api/content/{slug}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `slug` | string | Yes | The unique slug identifier for the content item |

#### Validation Rules

- `slug`: Max 200 characters
- Must contain only alphanumeric characters, hyphens, and underscores
- Cannot contain path traversal patterns (`..`, `/`, `\`)
- Cannot be null or empty

#### Response

**Status Code:** `200 OK`

**Content-Type:** `application/json`

**Body:**

```json
{
  "slug": "2024-01-15-hello-world",
  "title": "Hello World",
  "date": "2024-01-15T00:00:00",
  "author": "John Doe",
  "category": "Tutorial",
  "description": "A beginner's guide to Markdn",
  "tags": ["tutorial", "getting-started"],
  "content": "<h1>Hello World</h1>\n<p>Welcome to <strong>Markdn</strong>!</p>",
  "rawMarkdown": "# Hello World\n\nWelcome to **Markdn**!",
  "lastModified": "2024-01-15T10:30:00",
  "metadata": {
    "readTime": "2 min",
    "wordCount": 150
  }
}
```

#### Error Responses

**Status Code:** `404 Not Found`

Returned when:
- The slug does not exist
- The slug is invalid (fails validation)

**Example:**

```json
{
  "error": "Content not found"
}
```

**Status Code:** `500 Internal Server Error`

Returned when an unexpected error occurs. Check server logs for details.

#### Examples

**Get content by slug:**

```bash
curl http://localhost:5219/api/content/2024-01-15-hello-world
```

**Get content with date prefix:**

```bash
curl http://localhost:5219/api/content/2024-01-15-my-article
```

**Get content with simple slug:**

```bash
curl http://localhost:5219/api/content/about-us
```

---

### Health Check

Check the health status of the API.

```http
GET /api/health
```

#### Response

**Status Code:** `200 OK`

**Content-Type:** `application/json`

**Body:**

```json
{
  "status": "Healthy"
}
```

#### Examples

```bash
curl http://localhost:5219/api/health
```

---

## Response Models

### ContentListResponse

Container for paginated content listings.

```typescript
{
  items: ContentItemSummary[],
  pagination: PaginationMetadata
}
```

### ContentItemSummary

Summary of a content item (used in listings).

```typescript
{
  slug: string,              // Unique identifier
  title: string | null,      // Content title
  date: string | null,       // ISO 8601 date
  author: string | null,     // Author name
  category: string | null,   // Single category
  description: string | null, // Brief summary
  tags: string[]             // Array of tags
}
```

### ContentItemResponse

Full content item with rendered HTML.

```typescript
{
  slug: string,              // Unique identifier
  title: string | null,      // Content title
  date: string | null,       // ISO 8601 date
  author: string | null,     // Author name
  category: string | null,   // Single category
  description: string | null, // Brief summary
  tags: string[],            // Array of tags
  content: string,           // Rendered HTML
  rawMarkdown: string,       // Original Markdown
  lastModified: string,      // ISO 8601 timestamp
  metadata: object           // Additional metadata
}
```

### PaginationMetadata

Pagination information for list responses.

```typescript
{
  page: number,              // Current page (1-indexed)
  pageSize: number,          // Items per page
  totalItems: number,        // Total items across all pages
  totalPages: number         // Total number of pages
}
```

---

## Error Handling

### Standard Error Response

All error responses follow this format:

```json
{
  "error": "Error message",
  "details": "Optional detailed error information"
}
```

### HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful request |
| 400 | Bad Request | Invalid query parameters or validation failure |
| 404 | Not Found | Content with specified slug doesn't exist |
| 500 | Internal Server Error | Unexpected server error |

---

## Rate Limiting

Currently, Markdn does not implement rate limiting. For production deployments, consider implementing rate limiting at the reverse proxy level (nginx, Cloudflare, etc.).

---

## CORS

Markdn does not have CORS enabled by default. For web applications, configure CORS in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

---

## Content Negotiation

Currently, Markdn only returns JSON responses (`application/json`). Future versions may support additional formats:
- `text/html` - Rendered HTML
- `text/markdown` - Raw Markdown
- `text/plain` - Plain text (Markdown stripped)

---

## Caching Headers

Markdn currently does not set cache headers. For production, consider:

1. **Reverse Proxy Caching**: Use nginx/Cloudflare to cache GET responses
2. **CDN**: Deploy static content rendering behind a CDN
3. **Client-Side Caching**: Implement cache headers in middleware

Example cache header middleware:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Method == "GET")
    {
        context.Response.Headers.CacheControl = "public, max-age=300"; // 5 minutes
    }
    await next();
});
```

---

## Versioning

Current API version: **v1** (implicit in `/api/` prefix)

Future versions may introduce explicit versioning:
- Header-based: `X-API-Version: 2`
- URL-based: `/api/v2/content`

---

## OpenAPI/Swagger

In development mode, Swagger UI is available at:

```
http://localhost:5219/swagger
```

OpenAPI JSON specification:

```
http://localhost:5219/swagger/v1/swagger.json
```

To enable Swagger in production, modify `Program.cs`:

```csharp
// Remove the environment check
app.UseSwagger();
app.UseSwaggerUI();
```

**Warning:** Only enable Swagger in production if access is restricted (e.g., behind authentication).

---

## Client Libraries

### JavaScript/TypeScript

```typescript
interface ContentListResponse {
  items: ContentItemSummary[];
  pagination: PaginationMetadata;
}

async function getAllContent(params?: {
  tag?: string;
  category?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'date' | 'title' | 'lastmodified';
  sortOrder?: 'asc' | 'desc';
}): Promise<ContentListResponse> {
  const query = new URLSearchParams(params as any);
  const response = await fetch(`http://localhost:5219/api/content?${query}`);
  return response.json();
}

async function getContentBySlug(slug: string): Promise<ContentItemResponse> {
  const response = await fetch(`http://localhost:5219/api/content/${slug}`);
  if (!response.ok) throw new Error('Content not found');
  return response.json();
}
```

### Python

```python
import requests
from typing import Optional, Dict, Any

BASE_URL = "http://localhost:5219/api"

def get_all_content(
    tag: Optional[str] = None,
    category: Optional[str] = None,
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    page: int = 1,
    page_size: int = 50,
    sort_by: str = "date",
    sort_order: str = "desc"
) -> Dict[str, Any]:
    params = {
        "tag": tag,
        "category": category,
        "dateFrom": date_from,
        "dateTo": date_to,
        "page": page,
        "pageSize": page_size,
        "sortBy": sort_by,
        "sortOrder": sort_order
    }
    # Remove None values
    params = {k: v for k, v in params.items() if v is not None}
    
    response = requests.get(f"{BASE_URL}/content", params=params)
    response.raise_for_status()
    return response.json()

def get_content_by_slug(slug: str) -> Dict[str, Any]:
    response = requests.get(f"{BASE_URL}/content/{slug}")
    response.raise_for_status()
    return response.json()
```

---

## Best Practices

### Performance

1. **Use Pagination**: Always use `pageSize` appropriate for your use case
2. **Filter Early**: Apply filters to reduce data transfer
3. **Cache Aggressively**: Content rarely changes - cache responses
4. **Index Fields**: If extending to database, index `slug`, `category`, `tags`, `date`

### Security

1. **Validate Input**: Always validate and sanitize user input on the client side
2. **Use HTTPS**: Always use HTTPS in production
3. **Implement Rate Limiting**: Protect against abuse
4. **Monitor Logs**: Watch for suspicious patterns (path traversal attempts, etc.)

### Integration

1. **Handle 404s Gracefully**: Content may be deleted or moved
2. **Respect Pagination**: Don't try to fetch all items at once
3. **Use ETags**: Implement ETag support for efficient caching (future feature)
4. **Handle Dates Properly**: Parse ISO 8601 dates correctly in your timezone

---

## Support

For issues or questions:
- GitHub Issues: https://github.com/yourusername/markdn/issues
- Documentation: https://github.com/yourusername/markdn/docs
