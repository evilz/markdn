# Research: Markdn CMS Technology Choices

**Feature**: 001-markdown-cms-core  
**Date**: 2025-11-08  
**Purpose**: Document technology decisions and architectural patterns for the Markdn headless CMS

## Technology Stack Decisions

### Decision 1: Markdown Parser Library

**Decision**: Use **Markdig** for Markdown parsing

**Rationale**:
- Most feature-complete Markdown parser for .NET
- Supports CommonMark specification with GitHub Flavored Markdown (GFM) extensions
- Actively maintained with excellent performance
- Extensible pipeline architecture for custom processing
- Used by popular projects (DocFX, Statiq)

**Alternatives Considered**:
- **CommonMark.NET**: Less feature-complete, lacks GFM extensions
- **MarkdownSharp**: Unmaintained, older implementation
- **Custom parser**: Unnecessary complexity, specification compliance issues

**Implementation Notes**:
- Configure Markdig pipeline with GFM extensions: tables, task lists, strikethrough, autolinks
- Use `MarkdownPipelineBuilder` with `.UseAdvancedExtensions()` for full GFM support
- Async rendering not natively supported, wrap in `Task.Run` if needed for large files

---

### Decision 2: YAML Front-Matter Parser

**Decision**: Use **YamlDotNet** for YAML parsing

**Rationale**:
- De facto standard YAML library for .NET
- Robust, well-tested, actively maintained
- Supports YAML 1.1 specification
- Good error reporting for malformed YAML
- Flexible deserialization to both strongly-typed models and dictionaries

**Alternatives Considered**:
- **SharpYaml**: Less popular, smaller community
- **Regex-based custom parser**: Error-prone, incomplete YAML support
- **JSON.NET with YAML support**: Adds unnecessary dependency

**Implementation Notes**:
- Use `YamlDotNet.Serialization.Deserializer` with custom converters for dates
- Deserialize to `Dictionary<string, object>` for flexible custom fields
- Wrap parsing in try-catch to handle malformed YAML per FR-009
- Use `IDeserializer.Deserialize<T>()` for known fields, object for dynamic

---

### Decision 3: File System Watching

**Decision**: Use **FileSystemWatcher** (.NET built-in)

**Rationale**:
- Built into .NET, no external dependencies
- Cross-platform support (Windows, Linux, macOS)
- Event-driven architecture matches requirements
- Supports filtering by file extension (`.md`)
- Can watch subdirectories recursively

**Alternatives Considered**:
- **Polling**: Inefficient, higher latency, resource-intensive
- **Third-party libraries**: Unnecessary complexity for this use case

**Implementation Notes**:
- Watch for `Created`, `Changed`, `Deleted`, `Renamed` events
- Add debouncing (e.g., 500ms) to handle rapid successive changes
- Use `InternalBufferSize` increase if monitoring many files
- Handle `Error` event for buffer overflow scenarios
- Dispose properly with `await using` pattern

---

### Decision 4: API Framework

**Decision**: Use **ASP.NET Core Minimal APIs** (.NET 8)

**Rationale**:
- Lightweight, modern, high-performance
- Reduced boilerplate compared to MVC controllers
- Native support for dependency injection
- Built-in OpenAPI/Swagger support
- Excellent async/await support throughout
- Aligns with constitution's simplicity principle

**Alternatives Considered**:
- **ASP.NET Core MVC**: More boilerplate, overkill for API-only project
- **NancyFx**: Unmaintained, smaller ecosystem
- **Custom HTTP listener**: Reinventing the wheel, missing features (routing, DI, etc.)

**Implementation Notes**:
- Use route groups for organizing endpoints: `/api/content`, `/api/health`
- Leverage built-in model validation and binding
- Use `Results` API for typed responses: `Results.Ok()`, `Results.NotFound()`
- Configure `JsonSerializerOptions` for camelCase and null handling

---

### Decision 5: Testing Strategy

**Decision**: **xUnit + FluentAssertions** for all test types

**Rationale**:
- xUnit is the most modern, actively developed test framework for .NET
- Excellent async test support (critical for this project)
- FluentAssertions provides readable, expressive assertions
- Good IDE integration and test runner support
- Aligns with constitution's TDD requirements

**Alternatives Considered**:
- **NUnit**: Good but more verbose, older paradigm
- **MSTest**: Less feature-complete, less community support

**Test Organization**:
- **Unit Tests**: Test parsers, services in isolation with mocked dependencies
- **Integration Tests**: Test file system operations with test fixture files
- **Contract Tests**: Test API endpoints end-to-end with WebApplicationFactory

---

## Architectural Patterns

### Pattern 1: Repository Pattern (Minimal)

**Decision**: Use lightweight repository abstraction for file system access

**Rationale**:
- Testability: Easy to mock file system operations in unit tests
- Separation of concerns: File I/O logic isolated from business logic
- Not violating constitution: Only abstracting external dependency (file system)

**Implementation**:
```csharp
public interface IContentRepository
{
    Task<IEnumerable<FileInfo>> GetAllMarkdownFilesAsync(CancellationToken ct);
    Task<string> ReadFileContentAsync(string path, CancellationToken ct);
}
```

---

### Pattern 2: Service Layer

**Decision**: Separate service classes for content operations

**Rationale**:
- Coordinates between repository, parsers, and models
- Implements business rules (slug generation, filtering, pagination)
- Keeps controllers thin (constitution principle III: clean architecture)

**Services**:
- `IContentService`: Orchestrates content retrieval, filtering, pagination
- `IMarkdownParser`: Converts Markdown to HTML
- `IFrontMatterParser`: Extracts and parses YAML front-matter

---

### Pattern 3: Options Pattern

**Decision**: Use `IOptions<T>` for configuration

**Rationale**:
- .NET standard pattern for strongly-typed configuration
- Supports validation and reloading
- Testable with `Options.Create()`

**Configuration**:
```csharp
public class MarkdnOptions
{
    public string ContentDirectory { get; set; } = "content";
    public int MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
    public int DefaultPageSize { get; set; } = 50;
    public bool EnableFileWatching { get; set; } = true;
}
```

---

## Performance Considerations

### Caching Strategy

**Decision**: In-memory caching with invalidation on file changes

**Rationale**:
- Meets SC-001: <100ms response time for parsed content
- File system is the bottleneck, not parsing
- Simple invalidation via FileSystemWatcher events

**Implementation**:
- Use `IMemoryCache` (built-in)
- Key by file path or slug
- Invalidate on `Changed`, `Deleted` events
- Implement LRU with size limits to prevent memory exhaustion

---

### Async Streaming

**Decision**: Use async file streams for reading large files

**Rationale**:
- Non-blocking I/O per constitution principle IV
- Better scalability under concurrent load
- Handles files approaching 5MB limit efficiently

**Implementation**:
```csharp
await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
using var reader = new StreamReader(stream);
var content = await reader.ReadToEndAsync(cancellationToken);
```

---

## Security Considerations

### Path Traversal Prevention

**Pattern**: Validate all file paths to prevent directory traversal attacks

**Implementation**:
- Use `Path.GetFullPath()` and verify result starts with content directory
- Reject paths containing `..` or absolute paths from user input
- Whitelist `.md` extension only

---

### Input Validation

**Pattern**: Validate and sanitize all user inputs

**Implementation**:
- Enforce file size limits before reading (check `FileInfo.Length`)
- Validate date formats strictly (ISO 8601 only per clarifications)
- Sanitize filter parameters to prevent injection

---

## Logging Strategy

**Decision**: Use structured logging with ILogger

**Rationale**:
- Constitution requirement II: observability
- Native ASP.NET Core integration
- Works with Application Insights, Seq, Serilog sinks

**Log Levels**:
- **Information**: Startup, file changes detected, successful operations
- **Warning**: Malformed YAML, oversized files, slug collisions
- **Error**: File system errors, unexpected exceptions

---

## Deployment Considerations

### Containerization

**Decision**: Design for Docker deployment

**Configuration**:
- Content directory mounted as volume
- Environment variables for configuration
- Health check endpoint for orchestrator
- Non-root user in container

---

### Cross-Platform Testing

**Requirement**: Test on Windows and Linux file systems

**Considerations**:
- Case sensitivity differences (Linux vs Windows)
- Path separator handling (`Path.Combine` always)
- File locking behavior differences

---

## Open Questions for Design Phase

None - all clarifications resolved in spec clarification session.

---

## References

- Markdig: https://github.com/xoofx/markdig
- YamlDotNet: https://github.com/aaubry/YamlDotNet
- ASP.NET Core Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
- FileSystemWatcher: https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher
