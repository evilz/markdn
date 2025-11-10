# Implementation Plan: Content Collections

**Branch**: `002-content-collections` | **Date**: 2025-11-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-content-collections/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Content Collections provides a structured, type-safe way to manage and query content files (Markdown and JSON) with automatic schema validation. Collections are defined in a central configuration file (following Astro.build patterns) with schemas specifying required fields and types. The system validates content both eagerly at startup and lazily at runtime, supports OData-like query operations (filtering, sorting, pagination), and identifies content using slugs from front-matter with filename fallback. This feature extends the existing Markdn CMS with enhanced type safety and powerful querying capabilities.

## Technical Context

**Language/Version**: C# / .NET 8.0 (net8.0)  
**Primary Dependencies**: ASP.NET Core 8.0, Markdig (Markdown parsing), YamlDotNet (front-matter parsing), NJsonSchema 11.x (JSON schema validation)  
**Storage**: File system (existing Markdown/JSON files in collection folders)  
**Testing**: xUnit with ASP.NET Core TestHost (contract/integration tests), xUnit for unit tests  
**Target Platform**: Cross-platform (Linux, Windows, macOS) - containerized deployment  
**Project Type**: Web API (ASP.NET Core)  
**Performance Goals**: 200ms p95 for query operations with filters/sorting on collections up to 1000 items  
**Constraints**: Eager validation at startup must complete in under 5 seconds for 1000 items, async-first for all I/O operations  
**Scale/Scope**: Support 10+ collections with 1000+ items each, OData-like query syntax with filtering, sorting, and pagination

**Key Technical Decisions** (resolved in research.md):
- **JSON Schema Validation**: NJsonSchema 11.x - mature library with System.Text.Json integration and good performance
- **OData Query Parsing**: Custom lightweight parser supporting $filter, $orderby, $top, $skip subset (avoids Microsoft.AspNetCore.OData overhead)
- **Configuration Format**: JSON primary (collections.json) with optional YAML support if requested
- **Caching Strategy**: IMemoryCache with sliding expiration (5min for content, 1min for queries) and file change invalidation
- **File Watching**: FileSystemWatcher with 300ms debouncing and error resilience for runtime content change detection

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Test-First Development (TDD)
- ✅ **PASS**: Tests will be written first following Red-Green-Refactor cycle
- ✅ **PASS**: Contract tests for API endpoints defined before implementation
- ✅ **PASS**: Unit tests for validation logic, schema parsing, and query operations
- ✅ **PASS**: Integration tests for full collection workflow (define → validate → query)

### Principle II: Production-Ready by Default
- ✅ **PASS**: Input validation for schema definitions and query parameters
- ✅ **PASS**: Structured logging with ILogger for all operations
- ✅ **PASS**: Error handling with precise exception types for validation failures
- ✅ **PASS**: Timeouts on file I/O operations
- ⚠️ **REVIEW**: File watching for content changes - ensure resilience for file system events

### Principle III: Clean Code Architecture
- ✅ **PASS**: SOLID principles - separate concerns (schema loading, validation, querying)
- ✅ **PASS**: Minimal exposure - internal services, public API surface only
- ✅ **PASS**: No unnecessary abstractions - use existing ILogger, IOptions patterns
- ✅ **PASS**: Reuse existing content loading mechanisms from base Markdn CMS

### Principle IV: Async-First Programming
- ✅ **PASS**: All I/O operations async (file reading, validation)
- ✅ **PASS**: CancellationToken propagation throughout call chain
- ✅ **PASS**: Async method naming with Async suffix
- ✅ **PASS**: ConfigureAwait(false) in library code
- ✅ **PASS**: Streaming for large collections if needed

### Principle V: Performance & Cloud-Native
- ✅ **PASS**: Stream file reading to avoid loading all content into memory
- ✅ **PASS**: Configuration from environment variables (12-factor)
- ✅ **PASS**: Health checks for collection validation status
- ✅ **PASS**: Cross-platform file path handling
- ⚠️ **REVIEW**: Measure and optimize query performance with filtering/sorting
- ⚠️ **REVIEW**: Consider object pooling for validation result objects if profiling shows benefit

**Overall Status**: ✅ **APPROVED** - Proceed to Phase 0 with noted review items

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Markdn.Api/
├── Models/
│   ├── Collection.cs              # Collection entity with schema
│   ├── CollectionSchema.cs        # Schema definition with fields
│   ├── ContentItem.cs             # Validated content item
│   ├── FieldDefinition.cs         # Field type and constraints
│   └── ValidationResult.cs        # Validation outcome
├── Configuration/
│   ├── CollectionsConfiguration.cs # Configuration file model
│   └── CollectionsOptions.cs       # IOptions for collections
├── Services/
│   ├── ICollectionService.cs      # Query and retrieval operations
│   ├── CollectionService.cs       # Implementation
│   ├── ISchemaValidator.cs        # Validation interface
│   ├── SchemaValidator.cs         # Schema validation logic
│   ├── ICollectionLoader.cs       # Load schemas and content
│   └── CollectionLoader.cs        # Implementation
├── Validation/
│   ├── CollectionSchemaValidator.cs # FluentValidation for schemas
│   └── ContentItemValidator.cs      # Content validation against schema
├── Querying/
│   ├── IQueryParser.cs            # OData-like query parser
│   ├── QueryParser.cs             # Implementation
│   ├── QueryExpression.cs         # Parsed query representation
│   └── QueryExecutor.cs           # Execute queries on collections
├── Endpoints/
│   └── CollectionsEndpoints.cs    # Minimal API endpoints
└── HostedServices/
    └── CollectionValidationService.cs # Background validation on startup

tests/Markdn.Api.Tests.Unit/
├── Services/
│   ├── CollectionServiceTests.cs
│   ├── SchemaValidatorTests.cs
│   └── CollectionLoaderTests.cs
├── Validation/
│   ├── CollectionSchemaValidatorTests.cs
│   └── ContentItemValidatorTests.cs
└── Querying/
    ├── QueryParserTests.cs
    └── QueryExecutorTests.cs

tests/Markdn.Api.Tests.Integration/
├── CollectionWorkflowTests.cs     # End-to-end collection scenarios
└── CollectionQueryTests.cs        # Query filtering, sorting, pagination

tests/Markdn.Api.Tests.Contract/
└── Endpoints/
    └── CollectionsEndpointsTests.cs # API contract tests
```

**Structure Decision**: Single Web API project structure. Content Collections integrates into the existing `Markdn.Api` project, adding new models, services, and endpoints alongside the existing Markdown CMS functionality. This maintains project cohesion while clearly organizing collection-specific code in dedicated folders.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
