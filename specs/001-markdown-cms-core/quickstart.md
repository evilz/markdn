# Quickstart: Markdn CMS Development

**Feature**: 001-markdown-cms-core  
**Last Updated**: 2025-11-08

## Overview

This guide helps developers set up their environment and start working on the Markdn headless CMS.

## Prerequisites

- **.NET 8 SDK** (or later) - [Download](https://dotnet.microsoft.com/download)
- **Git** - For version control
- **Code Editor** - VS Code, Visual Studio, or Rider recommended
- **PowerShell** (Windows) or Bash (Linux/macOS) - For running scripts

### Verify Installation

```bash
dotnet --version  # Should show 8.0.x or higher
git --version
```

---

## Initial Setup

### 1. Clone Repository

```bash
git clone <repository-url>
cd markdn
```

### 2. Checkout Feature Branch

```bash
git checkout 001-markdown-cms-core
```

### 3. Restore Dependencies

```bash
dotnet restore
```

---

## Project Structure

```
markdn/
├── src/
│   └── Markdn.Api/              # Main API project
│       ├── Controllers/         # API endpoints
│       ├── Services/            # Business logic
│       ├── FileSystem/          # File operations
│       ├── Models/              # DTOs
│       └── Program.cs           # Entry point
├── tests/
│   ├── Markdn.Api.Tests.Unit/           # Unit tests
│   ├── Markdn.Api.Tests.Integration/    # Integration tests
│   └── Markdn.Api.Tests.Contract/       # API contract tests
└── specs/
    └── 001-markdown-cms-core/   # Design documents
        ├── spec.md              # Feature specification
        ├── plan.md              # Implementation plan (this phase)
        ├── data-model.md        # Data models
        ├── research.md          # Technology research
        └── contracts/           # API contracts
```

---

## Development Workflow (TDD)

**IMPORTANT**: This project follows strict Test-First Development (TDD) per the constitution.

### TDD Cycle

1. **Write Test** - Write a failing test that defines desired behavior
2. **Run Test** - Verify test fails (Red)
3. **Write Code** - Write minimal code to make test pass
4. **Run Test** - Verify test passes (Green)
5. **Refactor** - Improve code while keeping tests green
6. **Repeat** - Move to next behavior

### Example TDD Workflow

```bash
# 1. Write a failing unit test
# Edit: tests/Markdn.Api.Tests.Unit/Services/MarkdownParserTests.cs

# 2. Run tests to see it fail
dotnet test tests/Markdn.Api.Tests.Unit

# 3. Implement just enough code to pass
# Edit: src/Markdn.Api/Services/MarkdownParser.cs

# 4. Run tests to see it pass
dotnet test tests/Markdn.Api.Tests.Unit

# 5. Refactor if needed (tests stay green)

# 6. Commit
git add .
git commit -m "feat: implement markdown parsing"
```

---

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project

```bash
# Unit tests only
dotnet test tests/Markdn.Api.Tests.Unit

# Integration tests only
dotnet test tests/Markdn.Api.Tests.Integration

# Contract tests only
dotnet test tests/Markdn.Api.Tests.Contract
```

### Run Tests with Coverage

```bash
# Install coverage tool (one-time)
dotnet tool install -g dotnet-coverage

# Collect coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test

# View results (open coverage.cobertura.xml in VS Code with Coverage Gutters extension)
```

### Watch Mode (Auto-rerun on changes)

```bash
dotnet watch test --project tests/Markdn.Api.Tests.Unit
```

---

## Running the API Locally

### Configure Content Directory

Edit `src/Markdn.Api/appsettings.Development.json`:

```json
{
  "Markdn": {
    "ContentDirectory": "../../content",
    "MaxFileSizeBytes": 5242880,
    "DefaultPageSize": 50,
    "EnableFileWatching": true
  }
}
```

### Create Sample Content

```bash
# Create content directory
mkdir content
cd content

# Create sample Markdown file
cat > getting-started.md << 'EOF'
---
title: Getting Started with Markdn
date: 2025-11-08
author: John Doe
tags: [tutorial, basics]
category: documentation
description: Learn how to use Markdn CMS
---

# Getting Started

Welcome to Markdn CMS! This is a lightweight headless CMS that uses plain Markdown files.

## Features

- **No Database**: Content stored as Markdown files
- **Git-Friendly**: Version control your content
- **Fast**: In-memory caching with file watching
- **Flexible**: Custom front-matter fields supported

EOF
```

### Start API

```bash
dotnet run --project src/Markdn.Api
```

API will start at `http://localhost:5000` (or port shown in console).

### Test Endpoints

```bash
# Health check
curl http://localhost:5000/api/health

# List all content
curl http://localhost:5000/api/content

# Get specific content
curl http://localhost:5000/api/content/getting-started

# Filter by tag
curl "http://localhost:5000/api/content?tag=tutorial"

# Get only HTML
curl "http://localhost:5000/api/content/getting-started?format=html"
```

---

## Development Tips

### Hot Reload

Use `dotnet watch` for automatic restart on code changes:

```bash
dotnet watch run --project src/Markdn.Api
```

### Debugging in VS Code

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Markdn.Api/bin/Debug/net8.0/Markdn.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Markdn.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Swagger UI

Once API is running, open http://localhost:5000/swagger to explore the API interactively.

---

## Key Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| **Markdig** | Markdown parsing | Latest stable |
| **YamlDotNet** | YAML front-matter parsing | Latest stable |
| **xUnit** | Test framework | Latest stable |
| **FluentAssertions** | Readable assertions | Latest stable |
| **Microsoft.AspNetCore.Mvc.Testing** | Integration testing | 8.0.x |

---

## Code Style & Conventions

### Naming

- **Classes**: PascalCase (`ContentService`, `MarkdownParser`)
- **Interfaces**: IPascalCase (`IContentService`, `IMarkdownParser`)
- **Methods**: PascalCase (`GetAllContentAsync`, `ParseFrontMatter`)
- **Private fields**: `_camelCase` (`_contentCache`, `_logger`)
- **Parameters**: camelCase (`filePath`, `cancellationToken`)

### Async Methods

- All async methods end with `Async` suffix
- Always accept `CancellationToken` parameter (even if not used immediately)
- Use `ConfigureAwait(false)` in library code

```csharp
public async Task<ContentItem> GetContentAsync(string slug, CancellationToken cancellationToken)
{
    var content = await _repository.ReadFileAsync(slug, cancellationToken)
        .ConfigureAwait(false);
    return ParseContent(content);
}
```

### Error Handling

- Use specific exception types
- Never swallow exceptions silently
- Log errors with context

```csharp
try
{
    return await ParseYamlAsync(content, cancellationToken);
}
catch (YamlException ex)
{
    _logger.LogWarning(ex, "Failed to parse YAML in file {FilePath}", filePath);
    return new FrontMatter(); // Return empty with warnings
}
```

---

## Common Tasks

### Add New Endpoint

1. **Write contract test** (TDD step 1):
   ```csharp
   // tests/Markdn.Api.Tests.Contract/Endpoints/ContentEndpointsTests.cs
   [Fact]
   public async Task GetContentBySlug_ReturnsOk()
   {
       var response = await _client.GetAsync("/api/content/test-slug");
       response.StatusCode.Should().Be(HttpStatusCode.OK);
   }
   ```

2. **Run test** - Should fail (Red)

3. **Implement endpoint**:
   ```csharp
   // src/Markdn.Api/Program.cs
   app.MapGet("/api/content/{slug}", async (string slug, IContentService service) =>
   {
       var content = await service.GetBySlugAsync(slug);
       return content is not null ? Results.Ok(content) : Results.NotFound();
   });
   ```

4. **Run test** - Should pass (Green)

### Add New Service

1. **Write unit test** first
2. **Create interface** in `Services/`
3. **Implement class**
4. **Register in DI** (`Program.cs`):
   ```csharp
   builder.Services.AddSingleton<IContentService, ContentService>();
   ```

---

## Troubleshooting

### File Watching Not Working

**Issue**: Changes to Markdown files not detected

**Solution**: Check `FileSystemWatcher` configuration:
- Ensure `EnableFileWatching` is `true` in config
- Verify content directory path is correct
- Check file system permissions

### Tests Failing with File Locks

**Issue**: Integration tests fail due to file locking

**Solution**:
- Use `FileShare.Read` when opening files
- Dispose streams properly with `await using`
- Add small delay in tests if needed

### Swagger Not Loading

**Issue**: `/swagger` returns 404

**Solution**:
- Ensure `app.UseSwagger()` and `app.UseSwaggerUI()` are called in `Program.cs`
- Only enabled in Development environment by default

---

## Next Steps

1. **Review Constitution** - `.specify/memory/constitution.md`
2. **Read Feature Spec** - `specs/001-markdown-cms-core/spec.md`
3. **Study Data Model** - `specs/001-markdown-cms-core/data-model.md`
4. **Review API Contract** - `specs/001-markdown-cms-core/contracts/openapi.yaml`
5. **Start with Tests** - Begin TDD cycle per `/speckit.tasks` workflow

---

## Getting Help

- **Constitution Questions**: Review `.specify/memory/constitution.md`
- **API Design**: Check `specs/001-markdown-cms-core/contracts/openapi.yaml`
- **Data Models**: See `specs/001-markdown-cms-core/data-model.md`
- **Technology Choices**: Read `specs/001-markdown-cms-core/research.md`

---

## Useful Commands Cheat Sheet

```bash
# Development
dotnet run --project src/Markdn.Api
dotnet watch run --project src/Markdn.Api

# Testing
dotnet test                                    # All tests
dotnet test --filter "Category=Unit"          # Unit tests only
dotnet watch test                              # Auto-rerun on changes

# Coverage
dotnet-coverage collect -f cobertura -o coverage.xml dotnet test

# Build
dotnet build
dotnet build --configuration Release

# Clean
dotnet clean

# Blazor Hot Reload (003-blazor-markdown-components)
dotnet watch --project src/Markdn.Blazor.App  # Start with hot reload enabled
```

---

## Hot Reload Development (Blazor Markdown Components)

**Feature 003**: Markdown files auto-regenerate into Blazor components with hot reload support.

### How It Works

1. **Start Watch Mode**:
   ```bash
   cd src/Markdn.Blazor.App
   dotnet watch
   ```

2. **Edit Markdown Files**: Modify any `.md` file in `Pages/` folder:
   ```markdown
   ---
   url: /my-page
   title: My Dynamic Page
   ---
   
   # Hello World
   
   Current time: @DateTime.Now.ToString("HH:mm:ss")
   ```

3. **Automatic Updates**:
   - Source generator detects file change
   - Regenerates `*.md.g.cs` component
   - Hot reload updates browser **without full refresh**
   - Page state preserved (if applicable)

### What Triggers Hot Reload

✅ **Markdown content changes**
- HTML markup
- Inline Razor expressions (`@variable`, `@method()`)
- Component usage (`<Counter />`)

✅ **YAML front matter changes**
- `url`, `title`, `namespace`, etc.

✅ **@code block changes**
- Component logic
- Properties and methods

### Hot Reload Behavior

- **Speed**: Updates typically complete in <3 seconds
- **State**: Browser state preserved where possible
- **Feedback**: Terminal shows `🔥 Hot reload succeeded`
- **Errors**: Compiler errors prevent hot reload (shown in terminal)

### Limitations

- Changing base class or layout requires full restart
- Adding new dependencies requires rebuild
- Source generator code changes require restart

### Example Workflow

```bash
# Terminal 1: Start watch mode
dotnet watch --project src/Markdn.Blazor.App

# Terminal 2: Edit markdown
echo "New content: @DateTime.Now" >> Pages/Dynamic.md

# Terminal 1: Watch for output
# dotnet watch ⌚ File updated: .\Pages\Dynamic.md
# dotnet watch 🔥 [Markdn.Blazor.App (net8.0)] Hot reload succeeded.

# Browser: Auto-refreshes with new content
```

---

## Constitution Compliance Checklist

Before committing code, verify:

- ✅ Tests written **before** implementation (TDD)
- ✅ All tests pass (`dotnet test`)
- ✅ Async methods end with `Async`
- ✅ `CancellationToken` accepted and propagated
- ✅ `ConfigureAwait(false)` used in library code
- ✅ Errors logged with context
- ✅ No unused methods or parameters
- ✅ Input validation performed
- ✅ Least-exposure rule followed (prefer `private`)
