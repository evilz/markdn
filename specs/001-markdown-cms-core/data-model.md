# Data Model: Markdn CMS

**Feature**: 001-markdown-cms-core  
**Date**: 2025-11-08  
**Purpose**: Define domain entities, DTOs, and their relationships

## Domain Entities

### ContentItem

Represents a single Markdown document with its metadata and content.

**Properties**:

| Property | Type | Description | Validation |
|----------|------|-------------|------------|
| `Slug` | `string` | URL-safe unique identifier | Required, unique, alphanumeric with hyphens |
| `FilePath` | `string` | Absolute path to source file | Required, must exist, must be .md |
| `Title` | `string?` | Content title from front-matter | Optional, max 200 chars |
| `Date` | `DateTime?` | Publication date (ISO 8601) | Optional, must be valid DateTime |
| `Author` | `string?` | Author name from front-matter | Optional, max 100 chars |
| `Tags` | `List<string>` | Tags for categorization | Optional, each tag max 50 chars |
| `Category` | `string?` | Primary category | Optional, max 50 chars |
| `Description` | `string?` | Short description/summary | Optional, max 500 chars |
| `CustomFields` | `Dictionary<string, object>` | Additional front-matter fields | Optional, serializable to JSON |
| `MarkdownContent` | `string` | Raw Markdown body | Required |
| `HtmlContent` | `string?` | Rendered HTML (lazy-loaded) | Optional, generated on demand |
| `LastModified` | `DateTime` | File last modified timestamp | Required, from FileInfo |
| `FileSizeBytes` | `long` | File size in bytes | Required, max 5MB |
| `HasParsingErrors` | `bool` | Indicates YAML parsing failures | Required |
| `ParsingWarnings` | `List<string>` | Error messages if any | Optional |

**Business Rules**:
- Slug generation precedence: front-matter `slug` → filename → full path → error
- Files >5MB are rejected and excluded
- Invalid YAML results in empty metadata with warnings
- All date fields must be ISO 8601 format

---

### FrontMatter

Represents the YAML metadata section.

**Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string?` | Document title |
| `Date` | `string?` | Publication date (string, parsed separately) |
| `Author` | `string?` | Author name |
| `Tags` | `List<string>?` | List of tags |
| `Category` | `string?` | Primary category |
| `Description` | `string?` | Short description |
| `Slug` | `string?` | Custom slug override |
| `AdditionalProperties` | `Dictionary<string, object>` | Dynamic fields |

**Parsing Notes**:
- Deserialize to `Dictionary<string, object>` first
- Extract known fields with type conversion
- Store unknowns in `AdditionalProperties`
- Handle malformed YAML per FR-009

---

### ContentCollection

Represents a queryable, paginated collection of content items.

**Properties**:

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `List<ContentItem>` | Content items for current page |
| `TotalCount` | `int` | Total items matching query |
| `Page` | `int` | Current page number (1-indexed) |
| `PageSize` | `int` | Items per page |
| `TotalPages` | `int` | Total pages (calculated) |
| `HasPrevious` | `bool` | Whether previous page exists |
| `HasNext` | `bool` | Whether next page exists |

**Computed Properties**:
```csharp
public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
public bool HasPrevious => Page > 1;
public bool HasNext => Page < TotalPages;
```

---

## API Data Transfer Objects (DTOs)

### ContentItemResponse

Response DTO for single content item retrieval.

```csharp
public class ContentItemResponse
{
    public string Slug { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Category { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    public string MarkdownContent { get; set; }
    public string? HtmlContent { get; set; }
    public DateTime LastModified { get; set; }
    public List<string>? Warnings { get; set; } // Present if HasParsingErrors
}
```

---

### ContentListResponse

Response DTO for paginated content list.

```csharp
public class ContentListResponse
{
    public List<ContentItemSummary> Items { get; set; }
    public PaginationMetadata Pagination { get; set; }
}

public class ContentItemSummary
{
    public string Slug { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Category { get; set; }
    public string? Description { get; set; }
    public DateTime LastModified { get; set; }
}

public class PaginationMetadata
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
```

---

### ContentQueryRequest

Query parameters for filtering and pagination.

```csharp
public class ContentQueryRequest
{
    public string? Tag { get; set; }
    public string? Category { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "date"; // date, title, lastModified
    public string? SortOrder { get; set; } = "desc"; // asc, desc
}
```

**Validation**:
- `Page` must be ≥ 1
- `PageSize` must be 1-100
- `DateFrom` and `DateTo` must be valid ISO 8601
- `SortBy` must be in allowed list
- `SortOrder` must be "asc" or "desc"

---

### ErrorResponse

Standard error response format.

```csharp
public class ErrorResponse
{
    public string Error { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public string? Detail { get; set; }
}
```

---

## Entity Relationships

```
ContentRepository
    ↓ reads files
FileInfo → FrontMatterParser → FrontMatter
    ↓                               ↓
    ↓                         extracts metadata
    ↓                               ↓
FileContent → MarkdownParser → ContentItem
    ↓
    → ContentService → ContentCollection
                           ↓
                     ContentListResponse
```

**Flow**:
1. `ContentRepository` scans file system for `.md` files
2. For each file: read content, parse front-matter, parse Markdown
3. `ContentItem` aggregates all data
4. `ContentService` filters/pages items → `ContentCollection`
5. Map to DTOs for API responses

---

## Value Objects

### Slug

Represents a URL-safe content identifier.

**Rules**:
- Lowercase alphanumeric with hyphens
- No leading/trailing hyphens
- Max 200 characters
- Must be unique within repository

**Generation Algorithm**:
```csharp
public static string GenerateSlug(string input)
{
    // 1. Lowercase
    var slug = input.ToLowerInvariant();
    
    // 2. Replace spaces and underscores with hyphens
    slug = Regex.Replace(slug, @"[\s_]+", "-");
    
    // 3. Remove invalid characters
    slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
    
    // 4. Remove duplicate hyphens
    slug = Regex.Replace(slug, @"-{2,}", "-");
    
    // 5. Trim hyphens
    slug = slug.Trim('-');
    
    // 6. Limit length
    if (slug.Length > 200)
        slug = slug.Substring(0, 200).TrimEnd('-');
    
    return slug;
}
```

---

## Validation Rules

### File-Level Validation

| Rule | Check | Action on Failure |
|------|-------|-------------------|
| File exists | `File.Exists(path)` | Log error, skip file |
| Size limit | `FileInfo.Length <= 5MB` | Log warning, exclude from collection |
| Extension | `Path.GetExtension(path) == ".md"` | Skip file |
| Readable | Try file read | Log error, skip file |

---

### Front-Matter Validation

| Field | Validation | Default on Invalid |
|-------|------------|-------------------|
| `title` | String, max 200 chars | null |
| `date` | ISO 8601 DateTime | null |
| `author` | String, max 100 chars | null |
| `tags` | Array of strings | empty list |
| `category` | String, max 50 chars | null |
| `slug` | Alphanumeric + hyphens | generated from filename |

---

## State Transitions

### ContentItem Lifecycle

```
[File Created] → [Discovered] → [Parsed] → [Cached] → [Served]
                      ↓                        ↓
                  [Parse Error] ──────────→ [Served with Warnings]
                  
[File Modified] → [Cache Invalidated] → [Re-parsed] → [Re-cached]

[File Deleted] → [Cache Removed] → [404 on request]
```

---

## Indexing Strategy

### In-Memory Index

Maintain concurrent dictionary for fast lookups:

```csharp
private readonly ConcurrentDictionary<string, ContentItem> _contentBySlug;
private readonly ConcurrentDictionary<string, ContentItem> _contentByPath;
private readonly ConcurrentDictionary<string, List<ContentItem>> _contentByTag;
private readonly ConcurrentDictionary<string, List<ContentItem>> _contentByCategory;
```

**Index Updates**:
- On file created/changed: Update all relevant indexes
- On file deleted: Remove from all indexes
- On slug collision: Log error and reject new file

---

## Performance Considerations

### Lazy Loading

- **HTML content**: Only render when requested with `format=html`
- **Large fields**: Consider truncating descriptions in list views

### Caching

- Cache parsed `ContentItem` objects in `IMemoryCache`
- Cache key: file path (absolute)
- Expiration: Invalidate on FileSystemWatcher events
- Size limit: LRU with max 1000 items or 500MB

### Filtering Optimization

- Index by common filter fields (tag, category, date)
- For date range queries: Use sorted index
- For text search: Consider future full-text indexing

---

## Extension Points

Future enhancements (not in scope for this feature):

- **Search**: Full-text search across content and metadata
- **Related content**: Link to related items by tags/category
- **Drafts**: Support `draft: true` front-matter field
- **Multi-language**: Locale-specific content routing
- **Media**: Reference external media files

---

## Mapping Between Layers

### Domain → DTO Mapping

```csharp
public static ContentItemResponse ToResponse(ContentItem item, bool includeHtml)
{
    return new ContentItemResponse
    {
        Slug = item.Slug,
        Title = item.Title,
        Date = item.Date,
        Author = item.Author,
        Tags = item.Tags,
        Category = item.Category,
        Description = item.Description,
        CustomFields = item.CustomFields,
        MarkdownContent = item.MarkdownContent,
        HtmlContent = includeHtml ? item.HtmlContent : null,
        LastModified = item.LastModified,
        Warnings = item.HasParsingErrors ? item.ParsingWarnings : null
    };
}
```
