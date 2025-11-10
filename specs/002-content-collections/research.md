# Research: Content Collections

**Feature**: Content Collections  
**Date**: 2025-11-09  
**Purpose**: Resolve technical unknowns and establish best practices for implementation

## Research Topics

### 1. JSON Schema Validation Library for .NET

**Decision**: Use **NJsonSchema** (version 11.x)

**Rationale**:
- Mature, actively maintained library with strong JSON Schema Draft support
- Native integration with System.Text.Json and Newtonsoft.Json
- Provides both validation and schema generation capabilities
- Good performance characteristics for our scale (1000+ items)
- Wide adoption in .NET ecosystem (used by NSwag, AutoRest, etc.)

**Alternatives Considered**:
- **System.Text.Json.Schema**: Not yet available as stable package in .NET 8
- **Json.NET Schema (Newtonsoft)**: Requires license for commercial use, heavier dependency
- **Custom validation**: Too complex to build and maintain, reinventing the wheel

**Implementation Notes**:
- Use `JsonSchema` to define collection schemas
- Validate using `JsonSchema.Validate()` method
- Support JSON Schema Draft 7 or later
- Cache compiled schemas for performance

---

### 2. OData Query Parsing Approach

**Decision**: Build **lightweight custom query parser** supporting OData-like subset

**Rationale**:
- Full Microsoft.AspNetCore.OData is heavy (adds EDM model complexity, unused features)
- We only need subset: $filter, $orderby, $top, $skip, $select
- Custom parser gives control over supported operators and error messages
- Better performance for our specific use case (in-memory collections)
- Simpler testing and maintenance without full OData protocol overhead

**Alternatives Considered**:
- **Microsoft.AspNetCore.OData**: Too heavy, requires EDM model setup, overkill for file-based CMS
- **Dynamic LINQ**: Powerful but allows arbitrary code execution, security concerns
- **Third-party OData parsers**: Limited options with good maintenance

**Implementation Notes**:
- Support operators: eq, ne, gt, lt, ge, le, contains, startswith, endswith
- Support logical operators: and, or, not
- Support functions: tolower, toupper for case-insensitive comparisons
- Use recursive descent parser or ANTLR-generated parser
- Return strongly-typed expression trees for execution
- Validate field names against schema at parse time

**Supported Query Syntax**:
```
$filter=author eq 'John' and publishDate gt '2025-01-01'
$orderby=publishDate desc,title asc
$top=10
$skip=20
$select=title,author,publishDate
```

---

### 3. Configuration File Format

**Decision**: Support **JSON** (primary) with optional **YAML** support

**Rationale**:
- JSON is native to .NET serialization (System.Text.Json)
- JSON schema validation works natively with JSON config
- YAML is more human-friendly but adds dependency
- Start with JSON, add YAML support if users request it
- Follows Astro.build pattern (supports both)

**Alternatives Considered**:
- **JSON only**: Simplest, but less human-friendly for complex schemas
- **YAML only**: Requires YamlDotNet dependency, less native tooling support
- **TOML**: Less common in .NET ecosystem

**Implementation Notes**:
- Primary: `collections.json` in content root
- Optional: `collections.yaml` (check if present, load with YamlDotNet)
- Use IOptions pattern for strongly-typed configuration
- Example JSON structure:
```json
{
  "contentRootPath": "content",
  "collections": {
    "blog": {
      "folder": "blog",
      "schema": {
        "type": "object",
        "properties": {
          "title": { "type": "string" },
          "author": { "type": "string" },
          "publishDate": { "type": "string", "format": "date" },
          "tags": { "type": "array", "items": { "type": "string" } }
        },
        "required": ["title", "publishDate"]
      }
    }
  }
}
```

---

### 4. In-Memory Caching Strategy

**Decision**: Use **IMemoryCache** with sliding expiration and file change invalidation

**Rationale**:
- Built-in ASP.NET Core caching is lightweight and performant
- Sliding expiration keeps hot data in cache
- File system watcher invalidates cache on content changes
- Simple cache key strategy: `collection:{name}:{slug}`
- Good fit for read-heavy workload with occasional writes

**Alternatives Considered**:
- **No caching**: Simple but poor performance, re-validates on every request
- **Distributed cache (Redis)**: Overkill for single-instance CMS, adds infrastructure
- **Custom cache implementation**: Unnecessary complexity

**Implementation Notes**:
- Cache validated content items with 5-minute sliding expiration
- Cache query results with 1-minute absolute expiration
- Invalidate specific items or entire collections on file changes
- Cache schema definitions indefinitely (invalidate only on config change)
- Use `MemoryCacheEntryOptions` with size limit to prevent memory bloat

**Cache Keys**:
```
schema:{collectionName}
content:{collectionName}:{slug}
query:{collectionName}:{queryHash}
```

---

### 5. File Watcher Integration

**Decision**: Use **FileSystemWatcher** with debouncing and error resilience

**Rationale**:
- Native .NET file watching for content directories
- Detects changes at runtime for lazy validation
- Debouncing prevents event storms during bulk edits
- Resilient error handling for transient file system issues

**Alternatives Considered**:
- **Polling**: Simple but inefficient, high latency
- **Third-party libraries**: Unnecessary dependency for standard file watching
- **No runtime detection**: Forces restart for content changes

**Implementation Notes**:
- Watch collection folders for `*.md` and `*.json` files
- Watch configuration file for schema changes
- Debounce events (300ms) to batch rapid changes
- Use `NotifyFilters` = FileName, LastWrite, CreationTime
- Handle `Error` events gracefully (log and attempt to recover)
- Trigger background validation on detected changes
- Invalidate relevant cache entries
- Log all file change events for debugging

**Error Handling**:
- Network share disconnections: Log warning, attempt reconnection
- Rapid event storms: Debounce and batch
- Permission denied: Log error, skip file
- File locked: Retry with exponential backoff (3 attempts)

---

### 6. Best Practices for Schema Design

**Decision**: Follow **JSON Schema best practices** with Markdn-specific conventions

**Conventions**:
- Always include `$schema` reference to JSON Schema draft version
- Use descriptive `title` and `description` for schemas and fields
- Leverage `format` for dates, emails, URIs
- Use `pattern` for custom validation (e.g., slug format)
- Define reusable schema definitions with `$defs`
- Set reasonable `minLength`, `maxLength` for strings
- Use `enum` for closed value sets (status, category)

**Common Patterns**:
```json
{
  "slug": {
    "type": "string",
    "pattern": "^[a-z0-9-]+$",
    "description": "URL-friendly identifier"
  },
  "publishDate": {
    "type": "string",
    "format": "date",
    "description": "Publication date in ISO 8601 format"
  },
  "status": {
    "type": "string",
    "enum": ["draft", "published", "archived"]
  }
}
```

---

### 7. Performance Optimization Strategies

**Research Findings**:

**Query Optimization**:
- Build indexes on commonly filtered fields (author, date, tags)
- Use `Span<T>` for string operations in hot paths
- Lazy evaluation for large result sets
- Parallel validation with `Parallel.ForEachAsync` for large collections

**Validation Optimization**:
- Compile JSON schemas once at startup
- Validate incrementally (only changed files)
- Use `ValueTask<T>` for synchronous cache hits
- Batch validate during startup with progress reporting

**Memory Management**:
- Stream large Markdown files instead of loading entirely
- Use `ArrayPool<T>` for temporary buffers
- Dispose file streams properly with `await using`
- Implement IDisposable for validation contexts

**Measurement**:
- Add Activity Source for distributed tracing
- Log validation times and query performance
- Add custom metrics for cache hit rates
- Profile with BenchmarkDotNet for hot paths

---

## Summary

All technical unknowns have been resolved with clear decisions and rationale. The chosen technologies and patterns align with the .NET ecosystem, follow best practices, and meet the performance requirements (200ms p95 for queries, 5s startup validation for 1000 items). The implementation can proceed to Phase 1 (Design & Contracts).
