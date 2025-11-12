# Implementation Plan: Markdown to Razor Component Generator

**Branch**: `003-blazor-markdown-components` | **Date**: 2025-11-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-blazor-markdown-components/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement an MDX-like Markdown to Razor Component Generator as a C# incremental source generator that converts `.md` files into Blazor components (`.md.g.cs` files). The generator supports YAML front matter for metadata, embedded C# code blocks (`@code {}`), inline Razor expressions, component references, and parameterized components. It uses Markdig with a custom pipeline that preserves Razor syntax during Markdown parsing. Generated components work seamlessly across all Blazor rendering modes (Server, WebAssembly, SSR) with full hot reload support. The generator is implemented as an in-solution project referenced by the Blazor application, not distributed as a NuGet package.

## Technical Context

**Language/Version**: C# / .NET 8.0 (net8.0)  
**Primary Dependencies**: 
- Markdig (Markdown parsing - already in solution)
- YamlDotNet (YAML front matter parsing - already in solution)
- Microsoft.CodeAnalysis.CSharp (Roslyn for source generation)
- Microsoft.AspNetCore.Components (Blazor ComponentBase)

**Storage**: N/A (in-memory processing during compilation)  
**Testing**: xUnit with FluentAssertions  
**Target Platform**: Cross-platform (.NET 8.0) - Blazor Server, WebAssembly, SSR  
**Project Type**: Source generator library + Blazor integration  
**Performance Goals**: 
- Component generation <100ms per file during incremental compilation
- Support 100+ Markdown files without noticeable build time degradation
- Hot reload updates <3 seconds

**Constraints**: 
- Must integrate with existing Markdig/YamlDotNet usage
- Generated code must pass Blazor compilation pipeline
- Preserve Razor syntax through Markdown parsing without escaping
- No external NuGet dependencies for end users

**Scale/Scope**: 
- Support YAML front matter with 8 configuration keys
- Generate ComponentBase-derived classes with BuildRenderTree
- Handle nested directory structures and namespace generation
- Support all CommonMark + GitHub Flavored Markdown features

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Principle I: Test-First Development (TDD)
**Status**: COMPLIANT  
- Source generator will be developed with unit tests written first
- Test infrastructure: xUnit tests that compile generated source and verify output
- Generator tests will verify: YAML parsing, Markdown conversion, Razor preservation, parameter generation
- Integration tests will verify generated components compile and render correctly

### ✅ Principle II: Production-Ready by Default
**Status**: COMPLIANT  
**Security**:
- Input validation on YAML front matter (malformed YAML → clear error)
- No code execution during generation (only emit source code)
- Generated code uses standard Blazor security model (inherited from ComponentBase)

**Resilience**:
- File I/O errors handled gracefully with diagnostic messages
- Invalid Markdown syntax produces valid HTML (Markdig handles gracefully)
- Compilation errors in embedded C# reported by Roslyn naturally

**Observability**:
- Source generator diagnostic messages for all error conditions
- Generator emits structured diagnostics (severity, location, code)
- Build output shows clear messages for configuration errors

### ✅ Principle III: Clean Code Architecture
**Status**: COMPLIANT  
- Minimal abstraction: IIncrementalGenerator implementation, no unnecessary interfaces
- Reuse existing Markdig pipeline infrastructure
- Private-by-default visibility for generator internals
- No auto-generated code editing (generator outputs are consumed, not modified)
- Clear separation: parser → model → code emitter

### ✅ Principle IV: Async-First Programming
**Status**: COMPLIANT with EXCEPTION  
**Exception Justification**: Source generators run synchronously during compilation per Roslyn architecture. File I/O is minimal (reading `.md` files) and happens via Roslyn's `AdditionalText` API which is synchronous by design.
- No async/await in generator itself (not supported by IIncrementalGenerator)
- Generated component code will be async-ready (BuildRenderTree is synchronous per Blazor spec)

### ⚠️ Principle V: Performance & Cloud-Native
**Status**: COMPLIANT with CONSIDERATIONS  
- Generator uses incremental compilation (only regenerate changed files)
- Span<char> and Memory<char> for Markdown parsing where measured to help
- Cross-platform by design (.NET 8.0)
- No OS-specific APIs
- Configuration via project file (MSBuild properties)

**Consideration**: Source generators run in-process during build. Performance impact is development-time only, not runtime.

## Project Structure

### Documentation (this feature)

```text
specs/003-blazor-markdown-components/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── component-generation-schema.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Markdn.SourceGenerators/                    # NEW: Source generator project
│   ├── Markdn.SourceGenerators.csproj         # netstandard2.0 (generator requirement)
│   ├── MarkdownComponentGenerator.cs          # IIncrementalGenerator implementation
│   ├── Parsing/
│   │   ├── YamlFrontMatterParser.cs          # Extract YAML metadata
│   │   ├── MarkdownParser.cs                  # Markdig integration
│   │   └── RazorSyntaxPreserver.cs           # Custom Markdig extension
│   ├── CodeGen/
│   │   ├── ComponentCodeEmitter.cs            # Generate .cs source
│   │   ├── RenderTreeBuilder.cs              # BuildRenderTree method generation
│   │   └── ParameterGenerator.cs             # [Parameter] property generation
│   └── Models/
│       ├── MarkdownComponentModel.cs          # Parsed component representation
│       └── ComponentMetadata.cs               # YAML front matter model
│
├── Markdn.Blazor.App/                         # NEW: Example Blazor application
│   ├── Markdn.Blazor.App.csproj              # References SourceGenerators
│   ├── Pages/
│   │   ├── Home.md                            # Example routable page
│   │   └── About.md                           # Example with parameters
│   └── Components/
│       └── Greeting.md                        # Example non-routable component
│
└── Markdn.Api/                                # EXISTING: Main API (unchanged)

tests/
├── Markdn.SourceGenerators.Tests/            # NEW: Generator unit tests
│   ├── Markdn.SourceGenerators.Tests.csproj
│   ├── YamlFrontMatterParserTests.cs
│   ├── MarkdownParserTests.cs
│   ├── CodeEmitterTests.cs
│   ├── ParameterGenerationTests.cs
│   └── EndToEndGenerationTests.cs
│
└── Markdn.Blazor.App.Tests/                   # NEW: Integration tests
    ├── Markdn.Blazor.App.Tests.csproj
    ├── ComponentRenderingTests.cs
    └── HotReloadTests.cs
```

**Structure Decision**: New source generator project (`Markdn.SourceGenerators`) targeting netstandard2.0 per Roslyn requirements, referenced by new example Blazor app (`Markdn.Blazor.App`). Generator is separate from existing API to maintain clean separation. Test projects follow existing naming convention.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
