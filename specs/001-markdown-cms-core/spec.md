# Feature Specification: Markdn - Markdown-Based Headless CMS

**Feature Branch**: `001-markdown-cms-core`  
**Created**: 2025-11-08  
**Status**: Draft  
**Input**: User description: "lightweight, headless CMS that uses plain Markdown files as its only data source. It reads your .md documents (with optional front-matter), turns them into structured JSON, and serves them through a minimal ASP.NET API or Blazor UI. No database, no admin panel — just clean content you can version, edit, and deploy anywhere."

## Clarifications

### Session 2025-11-08

- Q: When two Markdown files in different directories have the same filename (e.g., `docs/intro.md` and `blog/intro.md`), how should the system generate unique identifiers/slugs? → A: Use slug from front-matter, then filename, then full path, then fail
- Q: What date format(s) should the system accept for the `date` front-matter field? → A: ISO 8601 format only (YYYY-MM-DD or YYYY-MM-DDTHH:MM:SS)
- Q: Should the API support pagination for large content collections? → A: Yes, support pagination with configurable page size (default 50 items)
- Q: When a Markdown file contains invalid/malformed YAML front-matter, what should the system do? → A: Log error, serve file with empty metadata, include warning in response
- Q: What should be the maximum file size limit for Markdown files? → A: 5 MB - ignore and fail if above

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Content Publisher Reading and Serving (Priority: P1)

A content creator has a directory of Markdown files with YAML front-matter (title, date, tags, etc.) and wants them automatically transformed into structured content accessible via an endpoint.

**Why this priority**: This is the core value proposition—reading Markdown files and serving them as structured data. Without this, the CMS cannot function.

**Independent Test**: Can be fully tested by placing Markdown files in a designated directory, starting the system, and retrieving content through an endpoint that returns structured JSON with front-matter metadata and parsed content.

**Acceptance Scenarios**:

1. **Given** a directory containing Markdown files with YAML front-matter, **When** the system starts, **Then** all Markdown files are discovered and indexed
2. **Given** a Markdown file with front-matter (title, date, author), **When** the file is requested, **Then** the system returns JSON containing the parsed front-matter as separate fields and the rendered content
3. **Given** multiple Markdown files, **When** the content list endpoint is called, **Then** all files are returned with their metadata in a structured collection
4. **Given** a Markdown file without front-matter, **When** the file is requested, **Then** the system returns JSON with the content and empty/default metadata fields

---

### User Story 2 - Content Query and Filtering (Priority: P2)

A developer wants to query content by metadata fields (tags, categories, date ranges) to build dynamic pages like "all blog posts tagged 'dotnet'" or "articles from 2025".

**Why this priority**: Enables practical use cases like blogs, documentation sites, and filtered content views. Essential for most real-world CMS applications.

**Independent Test**: Can be tested by creating Markdown files with various tags/categories in front-matter, then querying the system with filter parameters and verifying only matching content is returned.

**Acceptance Scenarios**:

1. **Given** Markdown files tagged with various categories, **When** a request filters by a specific tag, **Then** only files matching that tag are returned
2. **Given** Markdown files with publication dates, **When** a date range filter is applied, **Then** only files within that range are returned
3. **Given** Markdown files with multiple metadata fields, **When** multiple filters are combined (tag AND date range), **Then** only files matching all criteria are returned
4. **Given** a query with no matches, **When** the filter is applied, **Then** an empty collection is returned (not an error)

---

### User Story 3 - Live Content Updates (Priority: P3)

A content editor modifies a Markdown file on disk and expects the changes to be reflected immediately (or within seconds) without restarting the system.

**Why this priority**: Improves developer experience and enables live content workflows, but the system can function without it using manual restarts.

**Independent Test**: Can be tested by modifying a Markdown file while the system is running, then requesting that content and verifying the updated content is served.

**Acceptance Scenarios**:

1. **Given** the system is running and serving content, **When** a Markdown file is modified on disk, **Then** the system detects the change within a reasonable timeframe (e.g., 5 seconds)
2. **Given** a file has been modified, **When** the content is requested, **Then** the updated content is served (not cached old version)
3. **Given** a new Markdown file is added to the content directory, **When** the system rescans, **Then** the new file appears in the content collection
4. **Given** a Markdown file is deleted, **When** the system rescans, **Then** the file is removed from the content collection and returns appropriate not-found response

---

### User Story 4 - Content Rendering Options (Priority: P3)

A developer needs flexibility in how content is returned: raw Markdown, HTML-rendered content, or both, depending on the consuming application's needs.

**Why this priority**: Adds flexibility for different use cases (client-side rendering vs server-side rendering), but basic JSON with content is sufficient for MVP.

**Independent Test**: Can be tested by requesting the same content with different format parameters and verifying the response contains the requested format(s).

**Acceptance Scenarios**:

1. **Given** a Markdown file, **When** requested with default settings, **Then** both raw Markdown and HTML-rendered content are included in the response
2. **Given** a Markdown file, **When** requested with "format=markdown" parameter, **Then** only the raw Markdown content is returned
3. **Given** a Markdown file, **When** requested with "format=html" parameter, **Then** only the HTML-rendered content is returned
4. **Given** Markdown with code blocks and special formatting, **When** rendered to HTML, **Then** proper HTML elements are generated (code blocks, headers, lists, etc.)

---

### Edge Cases

- What happens when a Markdown file contains invalid YAML front-matter? (Log error with details, serve file with empty metadata, include warning field in JSON response)
- What happens when the content directory is empty? (System should return an empty collection, not fail)
- What happens when a Markdown file is extremely large (>5MB)? (Reject and exclude from collection, log rejection with file path and size)
- What happens with nested directory structures? (Recursively scan all subdirectories)
- What happens when two files have the same name in different directories? (Slug resolution follows precedence: front-matter slug field → filename → full relative path → fail with error logging if collision persists)
- What happens when a file is being written while the system tries to read it? (Handle file locking gracefully with retries or skip temporarily)
- What happens with different Markdown flavors/extensions? (CommonMark with GitHub Flavored Markdown extensions)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST read Markdown files from a configurable directory path
- **FR-002**: System MUST parse YAML front-matter from Markdown files when present
- **FR-003**: System MUST extract front-matter fields as separate, accessible metadata properties
- **FR-004**: System MUST parse Markdown content body and make it available as structured data
- **FR-005**: System MUST provide an endpoint that returns a list of all available content items with their metadata
- **FR-006**: System MUST provide an endpoint that returns a single content item by identifier (filename, slug, or path)
- **FR-007**: System MUST support filtering content by front-matter metadata fields (tags, categories, dates)
- **FR-008**: System MUST return content in JSON format
- **FR-009**: System MUST handle missing or malformed front-matter gracefully by serving the file with empty metadata, logging the error, and including a warning field in the JSON response
- **FR-010**: System MUST generate consistent, URL-safe identifiers (slugs) for each content item using this precedence: (1) custom slug field from front-matter, (2) filename without extension, (3) full relative path from content root, (4) fail with error if collision still exists
- **FR-011**: System MUST recursively scan subdirectories within the configured content path
- **FR-012**: System MUST render Markdown to HTML when requested
- **FR-013**: System MUST support file system watching to detect content changes without restart
- **FR-014**: System MUST handle concurrent read requests safely
- **FR-015**: System MUST provide appropriate HTTP status codes (200 OK, 404 Not Found, 500 Internal Server Error)
- **FR-016**: System MUST log parsing errors for invalid Markdown or front-matter files with sufficient detail for debugging (file path, error type, line number if available)
- **FR-017**: System MUST support common front-matter fields: title, date (ISO 8601 format: YYYY-MM-DD or YYYY-MM-DDTHH:MM:SS), author, tags, category, description, slug
- **FR-018**: System MUST allow custom front-matter fields to be accessible in the JSON response
- **FR-019**: System MUST support both relative and absolute file paths for content directory configuration
- **FR-020**: System MUST provide a health check endpoint to verify system readiness
- **FR-021**: System MUST support pagination for content collections with configurable page size (default 50 items per page)
- **FR-022**: System MUST include pagination metadata in responses (total count, current page, page size, total pages)
- **FR-023**: System MUST reject Markdown files larger than 5 MB, log the rejection with file path and size, and exclude them from the content collection

### Key Entities

- **ContentItem**: Represents a single Markdown document with its metadata (front-matter) and content body. Key attributes include: identifier/slug, title, date, author, tags, categories, raw Markdown content, HTML-rendered content, file path, last modified timestamp.

- **FrontMatter**: Represents the YAML metadata section at the beginning of a Markdown file. Contains key-value pairs for structured metadata (title, date, tags, etc.) and supports both standard and custom fields.

- **ContentCollection**: Represents a queryable collection of ContentItems with support for filtering, sorting, and pagination. Provides methods to retrieve all items or filter by metadata criteria. Includes pagination metadata (total count, current page, page size, total pages).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Content retrieval requests return results in under 100 milliseconds for files up to 1MB
- **SC-002**: System successfully parses and serves at least 1,000 Markdown files without performance degradation
- **SC-003**: File system changes are detected and reflected in content endpoints within 5 seconds
- **SC-004**: 100% of valid Markdown files with proper YAML front-matter are successfully parsed and served
- **SC-005**: System handles 100 concurrent read requests without errors or significant latency increase
- **SC-006**: Zero unhandled exceptions when processing malformed Markdown or front-matter (errors are logged but don't crash the system)
- **SC-007**: Content can be queried and filtered with response times under 200 milliseconds for collections up to 1,000 items
- **SC-008**: System startup time is under 5 seconds for repositories with up to 500 Markdown files

## Assumptions

- Content directory structure is managed externally (via Git, file sync, or manual editing)
- No authentication or authorization required for this phase (API is open or secured externally)
- Content files are UTF-8 encoded Markdown
- Front-matter uses YAML format (not TOML or JSON)
- Default Markdown flavor is CommonMark with GitHub Flavored Markdown (GFM) extensions
- Content versioning is handled externally (e.g., via Git)
- No content editing UI is provided in this phase (files are edited externally)
- The system runs on .NET 8 or later
- Deployment target is cross-platform (Windows, Linux, macOS)
- Content directory is on local file system (not remote storage like S3 or Azure Blob)
