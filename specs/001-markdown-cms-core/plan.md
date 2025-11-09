# Implementation Plan: Markdn - Markdown-Based Headless CMS

**Branch**: `001-markdown-cms-core` | **Date**: 2025-11-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-markdown-cms-core/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a lightweight headless CMS that reads Markdown files with YAML front-matter from a directory, parses them into structured JSON, and serves them through a REST API. The system supports file system watching for live updates, content filtering by metadata, pagination, and Markdown-to-HTML rendering. No database or admin UI—content management happens through direct file editing (Git-friendly workflow).

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core (Minimal API), Markdig (Markdown parser), YamlDotNet (YAML parser)  
**Storage**: File system (local directory with Markdown files)  
**Testing**: xUnit, FluentAssertions  
**Target Platform**: Cross-platform (Windows, Linux, macOS) - containerizable  
**Project Type**: Single web API project  
**Performance Goals**: <100ms response time for 1MB files, <200ms for filtered queries (1000 items), <5s startup (500 files)  
**Constraints**: 5MB max file size, ISO 8601 dates only, no database, stateless API design  
**Scale/Scope**: Handle 1,000+ Markdown files, 100 concurrent requests, file system watching enabled

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Test-First Development (TDD) — NON-NEGOTIABLE
✅ **PASS** - Plan includes comprehensive test structure (contract, integration, unit tests). Tests will be written before implementation per TDD workflow.

### Principle II: Production-Ready by Default
✅ **PASS** - Feature includes:
- Security: Input validation (file size limits, YAML parsing safety)
- Resilience: File system watching with error handling, graceful degradation for malformed files
- Observability: Structured logging for errors (FR-016), health check endpoint (FR-020)
- Error Handling: Specific error responses with warnings (FR-009)

### Principle III: Clean Code Architecture
✅ **PASS** - Simple, focused architecture:
- Clear separation: file reading, parsing, serving layers
- Minimal abstractions: no unnecessary interfaces
- SOLID principles applicable: SRP for parsers, services, and controllers

### Principle IV: Async-First Programming
✅ **PASS** - I/O-bound operations (file reading, HTTP requests) will use async/await throughout. File system watching is inherently async.

### Principle V: Performance & Cloud-Native
✅ **PASS** - Feature designed for cloud deployment:
- Cross-platform (no OS-specific code)
- Stateless API (horizontally scalable)
- Performance targets defined (SC-001 through SC-008)
- Health check endpoint for container orchestration

**Overall Status**: ✅ **ALL GATES PASSED** - No constitution violations detected in specification or design artifacts (research.md, data-model.md, contracts/openapi.yaml, quickstart.md). Proceed to implementation.

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
src/
├── Markdn.Api/              # ASP.NET Core Web API project
│   ├── Controllers/         # API endpoints
│   ├── Models/             # DTOs and response models
│   ├── Services/           # Business logic
│   │   ├── IContentService.cs
│   │   ├── ContentService.cs
│   │   ├── IMarkdownParser.cs
│   │   ├── MarkdownParser.cs
│   │   ├── IFrontMatterParser.cs
│   │   └── FrontMatterParser.cs
│   ├── FileSystem/         # File operations and watching
│   │   ├── IContentRepository.cs
│   │   ├── ContentRepository.cs
│   │   └── FileSystemWatcher.cs
│   ├── Configuration/      # Settings and options
│   ├── Middleware/         # Error handling, logging
│   └── Program.cs          # Entry point

tests/
├── Markdn.Api.Tests.Unit/        # Unit tests
│   ├── Services/
│   ├── Parsers/
│   └── FileSystem/
├── Markdn.Api.Tests.Integration/  # Integration tests
│   ├── Api/
│   └── FileSystem/
└── Markdn.Api.Tests.Contract/     # API contract tests
    └── Endpoints/
```

**Structure Decision**: Single web API project (Option 1) is appropriate because:
- No frontend in this phase (headless CMS)
- All logic is API-focused
- Simple deployment model
- Tests organized by type (unit, integration, contract) per constitution requirements
