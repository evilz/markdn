# Tasks: Markdn - Markdown-Based Headless CMS

**Input**: Design documents from `specs/001-markdown-cms-core/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: This feature follows TDD principles per the constitution. ALL test tasks MUST be completed BEFORE implementation tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

All paths are relative to repository root:
- Source: `src/Markdn.Api/`
- Tests: `tests/Markdn.Api.Tests.Unit/`, `tests/Markdn.Api.Tests.Integration/`, `tests/Markdn.Api.Tests.Contract/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution file at root: `Markdn.sln`
- [X] T002 Create ASP.NET Core Web API project: `src/Markdn.Api/Markdn.Api.csproj` with .NET 8 SDK
- [X] T003 [P] Create xUnit test project: `tests/Markdn.Api.Tests.Unit/Markdn.Api.Tests.Unit.csproj`
- [X] T004 [P] Create xUnit integration test project: `tests/Markdn.Api.Tests.Integration/Markdn.Api.Tests.Integration.csproj`
- [X] T005 [P] Create xUnit contract test project: `tests/Markdn.Api.Tests.Contract/Markdn.Api.Tests.Contract.csproj`
- [X] T006 Add Markdig package to `src/Markdn.Api/Markdn.Api.csproj`
- [X] T007 Add YamlDotNet package to `src/Markdn.Api/Markdn.Api.csproj`
- [X] T008 [P] Add FluentAssertions package to all test projects
- [X] T009 [P] Add Microsoft.AspNetCore.Mvc.Testing package to `tests/Markdn.Api.Tests.Integration/Markdn.Api.Tests.Integration.csproj`
- [X] T010 Create directory structure: `src/Markdn.Api/Models/`, `Services/`, `FileSystem/`, `Configuration/`, `Middleware/`
- [X] T011 Create `.editorconfig` at root with C# naming conventions per constitution
- [X] T012 Create sample content directory: `content/` with sample Markdown files for testing

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Configuration & Options

- [X] T013 Create `MarkdnOptions.cs` in `src/Markdn.Api/Configuration/` with ContentDirectory, MaxFileSizeBytes, DefaultPageSize, EnableFileWatching properties
- [X] T014 Add configuration binding in `src/Markdn.Api/Program.cs` for MarkdnOptions from appsettings.json
- [X] T015 Create `appsettings.json` and `appsettings.Development.json` in `src/Markdn.Api/` with Markdn configuration section

### Core Models & DTOs

- [X] T016 [P] Create `ContentItem.cs` domain entity in `src/Markdn.Api/Models/` with 20 properties per data-model.md
- [X] T017 [P] Create `FrontMatter.cs` entity in `src/Markdn.Api/Models/` with flexible schema support
- [X] T018 [P] Create `ContentCollection.cs` entity in `src/Markdn.Api/Models/` with pagination properties
- [X] T019 [P] Create `ContentItemResponse.cs` DTO in `src/Markdn.Api/Models/` per OpenAPI schema
- [X] T020 [P] Create `ContentListResponse.cs` DTO in `src/Markdn.Api/Models/` per OpenAPI schema
- [X] T021 [P] Create `ContentItemSummary.cs` DTO in `src/Markdn.Api/Models/` per OpenAPI schema
- [X] T022 [P] Create `PaginationMetadata.cs` DTO in `src/Markdn.Api/Models/` per OpenAPI schema
- [X] T023 [P] Create `ErrorResponse.cs` DTO in `src/Markdn.Api/Models/` per OpenAPI schema
- [X] T024 [P] Create `ContentQueryRequest.cs` DTO in `src/Markdn.Api/Models/` with filtering parameters

### Global Error Handling & Middleware

- [X] T025 Create `GlobalExceptionMiddleware.cs` in `src/Markdn.Api/Middleware/` for unhandled exceptions with structured logging
- [X] T026 Register GlobalExceptionMiddleware in `src/Markdn.Api/Program.cs` pipeline
- [X] T027 Configure Serilog or Microsoft.Extensions.Logging in `src/Markdn.Api/Program.cs` with structured output

### Health Check Infrastructure

- [X] T028 Create `HealthCheckService.cs` in `src/Markdn.Api/Services/` implementing content directory accessibility check
- [X] T029 Register health checks in `src/Markdn.Api/Program.cs` with dependency on HealthCheckService

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Content Publisher Reading and Serving (Priority: P1) 🎯 MVP

**Goal**: Read Markdown files from a directory, parse YAML front-matter and Markdown content, serve as structured JSON through REST API endpoints

**Independent Test**: Place Markdown files with front-matter in content directory, start API, call GET /content and GET /content/{slug} to retrieve structured JSON with metadata and content

### Unit Tests for User Story 1 (TDD - Write FIRST, verify FAIL)

- [X] T030 [P] [US1] Write failing unit test for FrontMatterParser with valid YAML in `tests/Markdn.Api.Tests.Unit/Services/FrontMatterParserTests.cs`
- [X] T031 [P] [US1] Write failing unit test for FrontMatterParser with invalid YAML in `tests/Markdn.Api.Tests.Unit/Services/FrontMatterParserTests.cs`
- [X] T032 [P] [US1] Write failing unit test for FrontMatterParser with missing front-matter in `tests/Markdn.Api.Tests.Unit/Services/FrontMatterParserTests.cs`
- [X] T033 [P] [US1] Write failing unit test for MarkdownParser HTML rendering in `tests/Markdn.Api.Tests.Unit/Services/MarkdownParserTests.cs`
- [X] T034 [P] [US1] Write failing unit test for MarkdownParser with GFM extensions in `tests/Markdn.Api.Tests.Unit/Services/MarkdownParserTests.cs`
- [X] T035 [P] [US1] Write failing unit test for slug generation from front-matter in `tests/Markdn.Api.Tests.Unit/Services/SlugGeneratorTests.cs`
- [X] T036 [P] [US1] Write failing unit test for slug generation from filename in `tests/Markdn.Api.Tests.Unit/Services/SlugGeneratorTests.cs`
- [X] T037 [P] [US1] Write failing unit test for slug generation from full path in `tests/Markdn.Api.Tests.Unit/Services/SlugGeneratorTests.cs`
- [X] T038 [P] [US1] Write failing unit test for ContentRepository reading Markdown file in `tests/Markdn.Api.Tests.Unit/FileSystem/ContentRepositoryTests.cs`
- [X] T039 [P] [US1] Write failing unit test for ContentRepository handling file >5MB in `tests/Markdn.Api.Tests.Unit/FileSystem/ContentRepositoryTests.cs`
- [X] T040 [P] [US1] Write failing unit test for ContentRepository recursive directory scan in `tests/Markdn.Api.Tests.Unit/FileSystem/ContentRepositoryTests.cs`
- [X] T041 [P] [US1] Write failing unit test for ContentService GetAll with pagination in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T042 [P] [US1] Write failing unit test for ContentService GetBySlug found case in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T043 [P] [US1] Write failing unit test for ContentService GetBySlug not found case in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`

### Contract Tests for User Story 1 (TDD - Write FIRST, verify FAIL)

- [X] T044 [P] [US1] Write failing contract test for GET /content returning 200 with ContentListResponse in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [X] T045 [P] [US1] Write failing contract test for GET /content with empty directory returning empty items array in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [X] T046 [P] [US1] Write failing contract test for GET /content/{slug} returning 200 with ContentItemResponse in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [X] T047 [P] [US1] Write failing contract test for GET /content/{slug} returning 404 for unknown slug in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [X] T048 [P] [US1] Write failing contract test for GET /health returning 200 with status healthy in `tests/Markdn.Api.Tests.Contract/Endpoints/HealthEndpointsTests.cs`

### Integration Tests for User Story 1 (TDD - Write FIRST, verify FAIL)

- [X] T049 [P] [US1] Write failing integration test for full file read → parse → serve workflow in `tests/Markdn.Api.Tests.Integration/ContentWorkflowTests.cs`
- [X] T050 [P] [US1] Write failing integration test for malformed YAML returning warnings in `tests/Markdn.Api.Tests.Integration/ContentWorkflowTests.cs`
- [X] T051 [P] [US1] Write failing integration test for file >5MB exclusion in `tests/Markdn.Api.Tests.Integration/ContentWorkflowTests.cs`

### Run Tests - Verify All FAIL (Red Phase)

- [X] T052 [US1] Run all User Story 1 tests with `dotnet test` and confirm ALL tests fail (Red phase of TDD)

### Implementation for User Story 1 (Green Phase)

- [X] T053 [P] [US1] Implement `IFrontMatterParser` interface in `src/Markdn.Api/Services/IFrontMatterParser.cs` with ParseAsync method
- [X] T054 [US1] Implement `FrontMatterParser` class in `src/Markdn.Api/Services/FrontMatterParser.cs` using YamlDotNet with error handling per FR-009
- [X] T055 [P] [US1] Implement `IMarkdownParser` interface in `src/Markdn.Api/Services/IMarkdownParser.cs` with RenderToHtmlAsync method
- [X] T056 [US1] Implement `MarkdownParser` class in `src/Markdn.Api/Services/MarkdownParser.cs` using Markdig with GFM extensions
- [X] T057 [P] [US1] Implement `ISlugGenerator` interface in `src/Markdn.Api/Services/ISlugGenerator.cs` with GenerateSlug method
- [X] T058 [US1] Implement `SlugGenerator` class in `src/Markdn.Api/Services/SlugGenerator.cs` with precedence logic per FR-010
- [X] T059 [P] [US1] Implement `IContentRepository` interface in `src/Markdn.Api/FileSystem/IContentRepository.cs` with GetAllFilesAsync, GetFileBySlugAsync
- [X] T060 [US1] Implement `ContentRepository` class in `src/Markdn.Api/FileSystem/ContentRepository.cs` with recursive scanning, 5MB validation, slug collision detection
- [X] T061 [P] [US1] Implement `IContentService` interface in `src/Markdn.Api/Services/IContentService.cs` with GetAllAsync, GetBySlugAsync
- [X] T062 [US1] Implement `ContentService` class in `src/Markdn.Api/Services/ContentService.cs` orchestrating repository, parsers, pagination logic
- [X] T063 [US1] Register all services in DI container in `src/Markdn.Api/Program.cs` (singletons for stateless, scoped where needed)
- [X] T064 [P] [US1] Implement GET /content endpoint in `src/Markdn.Api/Program.cs` using Minimal API with pagination parameters
- [X] T065 [P] [US1] Implement GET /content/{slug} endpoint in `src/Markdn.Api/Program.cs` using Minimal API
- [X] T066 [P] [US1] Implement GET /health endpoint in `src/Markdn.Api/Program.cs` using built-in health checks
- [X] T067 [US1] Add input validation for pagination parameters (page ≥1, pageSize 1-100) in endpoints
- [X] T068 [US1] Add structured logging to ContentService operations (file read, parse errors, slug collisions)
- [X] T069 [US1] Map domain entities to DTOs in ContentService (ContentItem → ContentItemResponse, ContentCollection → ContentListResponse)

### Run Tests - Verify All PASS (Green Phase)

- [X] T070 [US1] Run all User Story 1 tests with `dotnet test` and confirm ALL tests pass (Green phase of TDD)

### Refactor for User Story 1 (Refactor Phase)

- [X] T071 [US1] Refactor: Extract common parsing logic, ensure no code duplication
- [X] T072 [US1] Refactor: Review and optimize LINQ queries in ContentService
- [X] T073 [US1] Refactor: Ensure all async methods use ConfigureAwait(false)
- [X] T074 [US1] Run tests again to confirm refactoring didn't break functionality

**Checkpoint**: At this point, User Story 1 should be fully functional - can list all content and retrieve individual items with metadata

---

## Phase 4: User Story 2 - Content Query and Filtering (Priority: P2)

**Goal**: Enable developers to filter content by metadata fields (tags, categories, date ranges) through query parameters

**Independent Test**: Create Markdown files with various tags/categories/dates, call GET /content with filter parameters (tag, category, dateFrom, dateTo), verify only matching items returned

### Unit Tests for User Story 2 (TDD - Write FIRST, verify FAIL)

- [X] T075 [P] [US2] Write failing unit test for ContentService filter by single tag in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T076 [P] [US2] Write failing unit test for ContentService filter by category in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T077 [P] [US2] Write failing unit test for ContentService filter by date range in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T078 [P] [US2] Write failing unit test for ContentService filter by multiple criteria (tag AND category) in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T079 [P] [US2] Write failing unit test for ContentService sorting by date ascending in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T080 [P] [US2] Write failing unit test for ContentService sorting by date descending in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T081 [P] [US2] Write failing unit test for ContentService sorting by title in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [X] T082 [P] [US2] Write failing unit test for ContentService filter with no matches returning empty collection in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`

### Contract Tests for User Story 2 (TDD - Write FIRST, verify FAIL)

- [ ] T083 [P] [US2] Write failing contract test for GET /content?tag=tutorial returning filtered results in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T084 [P] [US2] Write failing contract test for GET /content?category=blog returning filtered results in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T085 [P] [US2] Write failing contract test for GET /content?dateFrom=2025-01-01&dateTo=2025-12-31 returning filtered results in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T086 [P] [US2] Write failing contract test for GET /content?tag=tutorial&category=blog with multiple filters in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T087 [P] [US2] Write failing contract test for GET /content?sortBy=date&sortOrder=asc returning sorted results in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T088 [P] [US2] Write failing contract test for GET /content with invalid date format returning 400 Bad Request in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`

### Integration Tests for User Story 2 (TDD - Write FIRST, verify FAIL)

- [ ] T089 [P] [US2] Write failing integration test for complex multi-filter query workflow in `tests/Markdn.Api.Tests.Integration/ContentFilteringTests.cs`
- [ ] T090 [P] [US2] Write failing integration test for pagination with filtered results in `tests/Markdn.Api.Tests.Integration/ContentFilteringTests.cs`

### Run Tests - Verify All FAIL (Red Phase)

- [X] T091 [US2] Run all User Story 2 tests with `dotnet test --filter "FullyQualifiedName~US2"` and confirm ALL tests fail

### Implementation for User Story 2 (Green Phase)

- [X] T092 [US2] Extend `IContentService.GetAllAsync` to accept ContentQueryRequest parameter in `src/Markdn.Api/Services/IContentService.cs`
- [X] T093 [US2] Implement tag filtering logic in `ContentService.GetAllAsync` using LINQ Where in `src/Markdn.Api/Services/ContentService.cs`
- [X] T094 [US2] Implement category filtering logic in `ContentService.GetAllAsync` in `src/Markdn.Api/Services/ContentService.cs`
- [X] T095 [US2] Implement date range filtering logic (dateFrom, dateTo) in `ContentService.GetAllAsync` in `src/Markdn.Api/Services/ContentService.cs`
- [X] T096 [US2] Implement sorting logic (sortBy, sortOrder) supporting date, title, lastModified in `src/Markdn.Api/Services/ContentService.cs`
- [X] T097 [US2] Update GET /content endpoint to bind query parameters to ContentQueryRequest in `src/Markdn.Api/Program.cs`
- [X] T098 [US2] Add date format validation (ISO 8601 only) for dateFrom/dateTo parameters in `src/Markdn.Api/Program.cs` or validation attribute
- [X] T099 [US2] Add validation for sortBy enum (date, title, lastModified) and sortOrder enum (asc, desc) in endpoint
- [X] T100 [US2] Add logging for filter operations (query parameters, result count) in ContentService

### Run Tests - Verify All PASS (Green Phase)

- [X] T101 [US2] Run all User Story 2 tests and confirm ALL tests pass

### Refactor for User Story 2 (Refactor Phase)

- [X] T102 [US2] Refactor: Extract filtering logic into separate FilterBuilder or QuerySpecification class if complex
- [X] T103 [US2] Refactor: Optimize filtering performance (consider indexing by tag/category if needed)
- [X] T104 [US2] Run tests again to confirm refactoring didn't break functionality

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - can list all content with filtering and pagination

---

## Phase 5: User Story 3 - Live Content Updates (Priority: P3)

**Goal**: Detect file system changes (create, modify, delete) to Markdown files and update in-memory cache automatically without restart

**Independent Test**: Start API with file watching enabled, modify a Markdown file on disk, wait 5 seconds, call GET /content/{slug} and verify updated content is returned

### Unit Tests for User Story 3 (TDD - Write FIRST, verify FAIL)

- [ ] T105 [P] [US3] Write failing unit test for FileWatcherService detecting file created event in `tests/Markdn.Api.Tests.Unit/FileSystem/FileWatcherServiceTests.cs`
- [ ] T106 [P] [US3] Write failing unit test for FileWatcherService detecting file changed event in `tests/Markdn.Api.Tests.Unit/FileSystem/FileWatcherServiceTests.cs`
- [ ] T107 [P] [US3] Write failing unit test for FileWatcherService detecting file deleted event in `tests/Markdn.Api.Tests.Unit/FileSystem/FileWatcherServiceTests.cs`
- [ ] T108 [P] [US3] Write failing unit test for FileWatcherService debouncing rapid changes (500ms window) in `tests/Markdn.Api.Tests.Unit/FileSystem/FileWatcherServiceTests.cs`
- [ ] T109 [P] [US3] Write failing unit test for ContentCache invalidation on file change in `tests/Markdn.Api.Tests.Unit/Services/ContentCacheTests.cs`
- [ ] T110 [P] [US3] Write failing unit test for ContentCache refresh after invalidation in `tests/Markdn.Api.Tests.Unit/Services/ContentCacheTests.cs`

### Integration Tests for User Story 3 (TDD - Write FIRST, verify FAIL)

- [ ] T111 [P] [US3] Write failing integration test for file modification detected and served within 5 seconds in `tests/Markdn.Api.Tests.Integration/FileWatchingTests.cs`
- [ ] T112 [P] [US3] Write failing integration test for new file creation detected and added to collection in `tests/Markdn.Api.Tests.Integration/FileWatchingTests.cs`
- [ ] T113 [P] [US3] Write failing integration test for file deletion detected and removed from collection in `tests/Markdn.Api.Tests.Integration/FileWatchingTests.cs`

### Run Tests - Verify All FAIL (Red Phase)

- [ ] T114 [US3] Run all User Story 3 tests and confirm ALL tests fail

### Implementation for User Story 3 (Green Phase)

- [ ] T115 [P] [US3] Implement `IContentCache` interface in `src/Markdn.Api/Services/IContentCache.cs` with Get, Set, Invalidate, RefreshAsync methods
- [ ] T116 [US3] Implement `ContentCache` class in `src/Markdn.Api/Services/ContentCache.cs` using IMemoryCache with thread-safe operations
- [ ] T117 [P] [US3] Implement `IFileWatcherService` interface in `src/Markdn.Api/FileSystem/IFileWatcherService.cs` with StartWatching, StopWatching
- [ ] T118 [US3] Implement `FileWatcherService` class in `src/Markdn.Api/FileSystem/FileWatcherService.cs` using FileSystemWatcher for .md files
- [ ] T119 [US3] Add debouncing logic (500ms timer) to FileWatcherService to handle rapid file changes
- [ ] T120 [US3] Wire FileWatcherService events (Created, Changed, Deleted) to ContentCache.InvalidateAsync in `src/Markdn.Api/FileSystem/FileWatcherService.cs`
- [ ] T121 [US3] Update ContentService to use ContentCache for Get operations in `src/Markdn.Api/Services/ContentService.cs`
- [ ] T122 [US3] Implement cache-aside pattern: check cache first, load from disk on miss, populate cache
- [ ] T123 [US3] Register IMemoryCache, ContentCache, FileWatcherService in DI container in `src/Markdn.Api/Program.cs`
- [ ] T124 [US3] Start FileWatcherService as IHostedService in `src/Markdn.Api/Program.cs` when EnableFileWatching is true
- [ ] T125 [US3] Add error handling for FileSystemWatcher.Error event (buffer overflow) with logging
- [ ] T126 [US3] Add logging for file watch events (file created, changed, deleted, cache invalidated)

### Run Tests - Verify All PASS (Green Phase)

- [ ] T127 [US3] Run all User Story 3 tests and confirm ALL tests pass

### Refactor for User Story 3 (Refactor Phase)

- [ ] T128 [US3] Refactor: Review FileWatcherService disposal, ensure proper cleanup with CancellationToken
- [ ] T129 [US3] Refactor: Consider extracting debouncing logic into reusable utility class
- [ ] T130 [US3] Run tests again to confirm refactoring didn't break functionality

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work - content updates automatically without restart

---

## Phase 6: User Story 4 - Content Rendering Options (Priority: P3)

**Goal**: Provide flexibility in content format returned (raw Markdown only, HTML only, or both) via query parameter

**Independent Test**: Request same content with different format parameters (format=markdown, format=html, format=both) and verify response contains only requested format(s)

### Unit Tests for User Story 4 (TDD - Write FIRST, verify FAIL)

- [ ] T131 [P] [US4] Write failing unit test for ContentService GetBySlugAsync with format=markdown returning only Markdown in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [ ] T132 [P] [US4] Write failing unit test for ContentService GetBySlugAsync with format=html returning only HTML in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [ ] T133 [P] [US4] Write failing unit test for ContentService GetBySlugAsync with format=both returning both formats in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`
- [ ] T134 [P] [US4] Write failing unit test for ContentService lazy-loading HTML (don't render unless requested) in `tests/Markdn.Api.Tests.Unit/Services/ContentServiceTests.cs`

### Contract Tests for User Story 4 (TDD - Write FIRST, verify FAIL)

- [ ] T135 [P] [US4] Write failing contract test for GET /content/{slug}?format=markdown returning only markdownContent field in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T136 [P] [US4] Write failing contract test for GET /content/{slug}?format=html returning only htmlContent field in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T137 [P] [US4] Write failing contract test for GET /content/{slug}?format=both returning both fields in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T138 [P] [US4] Write failing contract test for GET /content/{slug} with default format returning both fields in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`
- [ ] T139 [P] [US4] Write failing contract test for GET /content/{slug}?format=invalid returning 400 Bad Request in `tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs`

### Integration Tests for User Story 4 (TDD - Write FIRST, verify FAIL)

- [ ] T140 [P] [US4] Write failing integration test for Markdown with code blocks rendered correctly to HTML in `tests/Markdn.Api.Tests.Integration/ContentRenderingTests.cs`
- [ ] T141 [P] [US4] Write failing integration test for GFM tables rendered correctly to HTML in `tests/Markdn.Api.Tests.Integration/ContentRenderingTests.cs`

### Run Tests - Verify All FAIL (Red Phase)

- [ ] T142 [US4] Run all User Story 4 tests and confirm ALL tests fail

### Implementation for User Story 4 (Green Phase)

- [ ] T143 [US4] Add FormatOption enum (Markdown, Html, Both) in `src/Markdn.Api/Models/FormatOption.cs`
- [ ] T144 [US4] Update `IContentService.GetBySlugAsync` signature to accept FormatOption parameter in `src/Markdn.Api/Services/IContentService.cs`
- [ ] T145 [US4] Implement conditional HTML rendering in `ContentService.GetBySlugAsync` based on format parameter in `src/Markdn.Api/Services/ContentService.cs`
- [ ] T146 [US4] Update ContentItemResponse DTO mapping to set htmlContent=null when format=markdown in `src/Markdn.Api/Services/ContentService.cs`
- [ ] T147 [US4] Update ContentItemResponse DTO mapping to set markdownContent=null when format=html in `src/Markdn.Api/Services/ContentService.cs`
- [ ] T148 [US4] Update GET /content/{slug} endpoint to bind format query parameter to FormatOption in `src/Markdn.Api/Program.cs`
- [ ] T149 [US4] Add validation for format enum (markdown, html, both) in endpoint with 400 response for invalid values
- [ ] T150 [US4] Add performance optimization: skip MarkdownParser.RenderToHtmlAsync when format=markdown
- [ ] T151 [US4] Add logging for format selection in ContentService

### Run Tests - Verify All PASS (Green Phase)

- [ ] T152 [US4] Run all User Story 4 tests and confirm ALL tests pass

### Refactor for User Story 4 (Refactor Phase)

- [ ] T153 [US4] Refactor: Consider caching HTML content separately from Markdown to avoid re-rendering
- [ ] T154 [US4] Refactor: Review DTO mapping logic, consider using AutoMapper if mappings become complex
- [ ] T155 [US4] Run tests again to confirm refactoring didn't break functionality

**Checkpoint**: All user stories should now be independently functional - MVP + all P2/P3 features complete

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and finalize the feature

### Performance Optimization

- [ ] T156 [P] Benchmark GET /content with 1,000 files and verify <200ms response time per SC-007
- [ ] T157 [P] Benchmark GET /content/{slug} with 1MB file and verify <100ms response time per SC-001
- [ ] T158 [P] Benchmark startup time with 500 files and verify <5s per SC-008
- [ ] T159 Optimize LINQ queries in ContentService if benchmarks fail targets
- [ ] T160 Add caching strategy review: consider pre-loading cache on startup

### Security Hardening

- [X] T161 [P] Add path traversal prevention in ContentRepository (reject paths with .., absolute paths outside content directory)
- [X] T162 [P] Add input sanitization for slug parameter (max length, allowed characters)
- [X] T163 [P] Review and validate all user inputs for injection vulnerabilities
- [X] T164 Add security headers middleware (X-Content-Type-Options, X-Frame-Options, CSP, etc.)

### Documentation

- [X] T165 [P] Create API documentation in `docs/api.md` based on OpenAPI contract
- [X] T166 [P] Create deployment guide in `docs/deployment.md` for Docker, Kubernetes
- [X] T167 [P] Update root `README.md` with feature overview, quickstart, architecture diagram
- [X] T168 [P] Add XML documentation comments to all public interfaces and methods
- [X] T169 Configure Swagger/OpenAPI generation in `src/Markdn.Api/Program.cs` with examples

### Code Quality

- [X] T170 Run full test suite with code coverage: `dotnet test --collect:"XPlat Code Coverage"`
- [X] T171 Review coverage report, aim for >80% line coverage for all non-trivial code
- [X] T172 [P] Run static analysis with dotnet format: `dotnet format --verify-no-changes`
- [X] T173 [P] Review and fix any SonarLint or Roslyn analyzer warnings
- [X] T174 Add missing CancellationToken propagation to any remaining async methods

### Final Validation

- [X] T175 Follow quickstart.md step-by-step and verify all instructions work
- [X] T176 Test API manually with sample content directory containing 100+ files
- [X] T177 Test error scenarios: missing content directory, corrupted Markdown files, file system permission errors
- [X] T178 Verify health check endpoint returns correct status in various scenarios
- [X] T179 Verify logging output is structured and contains sufficient detail for debugging

### Cleanup

- [X] T180 Remove any unused dependencies from all .csproj files
- [X] T181 Remove any commented-out code or TODO comments
- [X] T182 Verify all files follow .editorconfig conventions
- [X] T183 Final commit with message "feat(001): complete markdown-cms-core feature"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - MVP foundation
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2), integrates with US1 but independently testable
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2), extends US1 with caching, independently testable
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2), extends US1 with format options, independently testable
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories - MVP READY after this
- **User Story 2 (P2)**: Can start after Foundational - Uses US1 endpoints but adds filtering - independently testable
- **User Story 3 (P3)**: Can start after Foundational - Adds caching layer to US1 - independently testable
- **User Story 4 (P3)**: Can start after Foundational - Extends US1 GetBySlug with format option - independently testable

### Within Each User Story (TDD Workflow)

1. **Write ALL tests first** (unit, contract, integration) - marked with [P] can be written in parallel
2. **Run tests** - verify ALL fail (Red phase)
3. **Implement interfaces** (marked [P] can be done in parallel)
4. **Implement classes** (may have dependencies on interfaces)
5. **Implement endpoints** (depend on services)
6. **Run tests** - verify ALL pass (Green phase)
7. **Refactor** - improve code while keeping tests green
8. **Checkpoint** - story is complete and independently deployable

### Parallel Opportunities

- **Setup Phase**: Tasks T003-T005, T006-T008 can run in parallel (different projects)
- **Foundational Phase**: All DTO creation (T016-T024) can run in parallel (different files)
- **Within Each User Story**: 
  - All test writing (marked [P]) can be done in parallel
  - All interface definitions (marked [P]) can be done in parallel
  - Different test projects can be worked on in parallel
- **Across User Stories**: After Foundational phase completes, US1, US2, US3, US4 can all be worked on by different developers in parallel

---

## Parallel Example: User Story 1 Test Writing

```bash
# All these tests can be written simultaneously by different developers or AI agents:

Task T030: Write FrontMatterParser valid YAML test
Task T031: Write FrontMatterParser invalid YAML test  
Task T032: Write FrontMatterParser missing front-matter test
Task T033: Write MarkdownParser HTML rendering test
Task T034: Write MarkdownParser GFM extensions test
Task T035: Write slug generation from front-matter test
Task T036: Write slug generation from filename test
Task T037: Write slug generation from path test
... (all marked with [P] in Phase 3)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only) - RECOMMENDED START

**Goal**: Deliver core value quickly

1. ✅ Complete **Phase 1: Setup** (T001-T012) - ~30 minutes
2. ✅ Complete **Phase 2: Foundational** (T013-T029) - ~2 hours
   - **CHECKPOINT**: Foundation ready, tests can run
3. ✅ Complete **Phase 3: User Story 1** (T030-T074) - ~4 hours
   - Write tests → Run (should fail) → Implement → Run (should pass) → Refactor
   - **CHECKPOINT**: MVP COMPLETE - can list content, retrieve by slug, basic API functional
4. 🚀 **DEPLOY MVP**: Validate with real users, gather feedback
5. 📊 **MEASURE**: Verify SC-001 (response time), SC-004 (parsing success rate)

**Why this works**: User Story 1 delivers complete end-to-end value - developers can use the API immediately even without filtering or file watching.

---

### Incremental Delivery (Add Stories Sequentially)

**After MVP (US1) is validated:**

1. ✅ Add **User Story 2** (Phase 4: T075-T104) - ~2 hours
   - **DEPLOY**: Now users can filter content by metadata
   - **MEASURE**: Verify SC-007 (filtered query response time)

2. ✅ Add **User Story 3** (Phase 5: T105-T130) - ~3 hours
   - **DEPLOY**: Now content updates automatically without restart
   - **MEASURE**: Verify SC-003 (change detection within 5 seconds)

3. ✅ Add **User Story 4** (Phase 6: T131-T155) - ~2 hours
   - **DEPLOY**: Now users can choose content format
   - **MEASURE**: Verify format=markdown improves response time

4. ✅ Complete **Polish** (Phase 7: T156-T183) - ~3 hours
   - **FINAL DEPLOYMENT**: Production-ready with docs, security, performance validated

**Benefits**:
- Each deployment adds value without breaking existing functionality
- Can pause at any checkpoint based on priorities
- Independent testing ensures no regressions
- Users get incremental improvements

---

### Parallel Team Strategy (Maximum Speed)

**If multiple developers available:**

1. **All together**: Complete Setup + Foundational (Phases 1-2)
   - **CHECKPOINT**: Foundation ready (~2.5 hours)

2. **Parallel execution** (after Foundation complete):
   - **Developer A**: User Story 1 (Phase 3) - 4 hours → MVP ready first
   - **Developer B**: User Story 2 (Phase 4) - 2 hours → Ready for integration
   - **Developer C**: User Story 3 (Phase 5) - 3 hours → Ready for integration
   - **Developer D**: User Story 4 (Phase 6) - 2 hours → Ready for integration

3. **Integration**: Merge all stories (minimal conflicts due to independence)

4. **All together**: Polish phase (Phase 7) - 3 hours

**Timeline**: ~9.5 hours total (vs ~16 hours sequential)

---

## Constitution Compliance Checklist

Before marking any phase complete, verify:

- ✅ **Tests written BEFORE implementation** (TDD Red-Green-Refactor)
- ✅ **All tests pass** (`dotnet test` shows 0 failures)
- ✅ **Async methods end with `Async` suffix**
- ✅ **`CancellationToken` accepted and propagated** in all async I/O operations
- ✅ **`ConfigureAwait(false)` used** in library code (Services, FileSystem)
- ✅ **Errors logged with context** (file path, operation, error details)
- ✅ **No unused methods or parameters** (Roslyn analyzer warnings addressed)
- ✅ **Input validation performed** (file size, date format, slug format, pagination params)
- ✅ **Least-exposure rule followed** (prefer `private`, `internal` over `public`)
- ✅ **Structured logging** (use ILogger with message templates, not string concatenation)

---

## Notes

- **[P] marker**: Indicates parallelizable tasks (different files, no blocking dependencies)
- **[US#] label**: Maps task to specific user story for traceability and independent delivery
- **TDD workflow**: Write tests → Run (fail) → Implement → Run (pass) → Refactor → Commit
- **Independent testing**: Each user story has its own test suite and can be validated in isolation
- **Commit strategy**: Commit after each checkpoint (Red phase, Green phase, Refactor phase per story)
- **Stop at any checkpoint**: Each user story completion is a valid stopping point for feedback/deployment
- **Total tasks**: 183 tasks organized across 7 phases
- **Estimated effort**: ~16 hours sequential, ~9.5 hours with parallel team
- **MVP scope**: Phases 1-3 (User Story 1 only) - ~6.5 hours

---

## Task Count Summary

- **Phase 1 (Setup)**: 12 tasks
- **Phase 2 (Foundational)**: 17 tasks  
- **Phase 3 (User Story 1 - P1)**: 45 tasks (14 unit tests, 5 contract tests, 3 integration tests, 22 implementation, 1 refactor check)
- **Phase 4 (User Story 2 - P2)**: 30 tasks (8 unit tests, 6 contract tests, 2 integration tests, 13 implementation, 1 refactor check)
- **Phase 5 (User Story 3 - P3)**: 26 tasks (6 unit tests, 3 integration tests, 16 implementation, 1 refactor check)
- **Phase 6 (User Story 4 - P3)**: 25 tasks (4 unit tests, 5 contract tests, 2 integration tests, 13 implementation, 1 refactor check)
- **Phase 7 (Polish)**: 28 tasks (performance, security, docs, quality, validation, cleanup)

**Total**: 183 tasks

**Test tasks**: 58 (32% of total) - following strict TDD per constitution
**Parallelizable tasks**: 87 marked with [P] (48% of total)
**MVP tasks (Phases 1-3)**: 74 tasks (40% of total)
