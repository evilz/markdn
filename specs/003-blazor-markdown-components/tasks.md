# Tasks: Markdown to Razor Component Generator

**Branch**: `003-blazor-markdown-components`  
**Input**: Design documents from `/specs/003-blazor-markdown-components/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/component-generation-schema.md

**Tests**: Test tasks are OPTIONAL and only included if explicitly requested. This feature specification does not request TDD approach, so test tasks are omitted. Tests will be written after implementation per standard development workflow.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Per plan.md, this feature creates two new projects:
- **Source Generator**: `src/Markdn.SourceGenerators/` (netstandard2.0)
- **Test App**: `src/Markdn.Blazor.App/` (net8.0 Blazor Server)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create source generator project at src/Markdn.SourceGenerators/Markdn.SourceGenerators.csproj with netstandard2.0 target
- [x] T002 Add Microsoft.CodeAnalysis.CSharp package (4.8.0) to Markdn.SourceGenerators project
- [x] T003 [P] Create test Blazor Server app at src/Markdn.Blazor.App/Markdn.Blazor.App.csproj with net8.0 target
- [x] T004 [P] Add project reference from Markdn.Blazor.App to Markdn.SourceGenerators with OutputItemType="Analyzer" and ReferenceOutputAssembly="false"
- [x] T005 [P] Create test content directory structure in src/Markdn.Blazor.App/Pages/ and src/Markdn.Blazor.App/Components/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 Create MarkdownComponentModel entity in src/Markdn.SourceGenerators/Models/MarkdownComponentModel.cs
- [x] T007 [P] Create ComponentMetadata entity in src/Markdn.SourceGenerators/Models/ComponentMetadata.cs
- [x] T008 [P] Create ParameterDefinition entity in src/Markdn.SourceGenerators/Models/ParameterDefinition.cs
- [x] T009 [P] Create MarkdownContent entity in src/Markdn.SourceGenerators/Models/MarkdownContent.cs
- [x] T010 [P] Create HtmlSegment entity with SegmentType enum in src/Markdn.SourceGenerators/Models/HtmlSegment.cs
- [x] T011 [P] Create ComponentReference entity in src/Markdn.SourceGenerators/Models/ComponentReference.cs
- [x] T012 [P] Create ComponentParameter entity in src/Markdn.SourceGenerators/Models/ComponentParameter.cs
- [x] T013 [P] Create CodeBlock entity in src/Markdn.SourceGenerators/Models/CodeBlock.cs
- [x] T014 [P] Create SourceLocation entity in src/Markdn.SourceGenerators/Models/SourceLocation.cs
- [x] T014 [P] Create HotReloadDetector in src/Markdn.SourceGenerators/Services/HotReloadDetector.cs
- [x] T015 Create YamlFrontMatterParser in src/Markdn.SourceGenerators/Parsers/YamlFrontMatterParser.cs with YamlDotNet integration
- [x] T016 Create MarkdigPipelineBuilder in src/Markdn.SourceGenerators/Parsers/MarkdigPipelineBuilder.cs
- [x] T017 Create RazorCodePreserver in src/Markdn.SourceGenerators/Parsers/RazorCodePreserver.cs
- [x] T018 Create RazorExpressionPreserver in src/Markdn.SourceGenerators/Parsers/RazorExpressionPreserver.cs
- [x] T019 Create RazorComponentPreserver in src/Markdn.SourceGenerators/Parsers/RazorComponentPreserver.cs
- [x] T020 Create MarkdownComponentParser in src/Markdn.SourceGenerators/Parsers/MarkdownComponentParser.cs
- [x] T021 Create DiagnosticDescriptors in src/Markdn.SourceGenerators/Diagnostics/DiagnosticDescriptors.cs
- [x] T022 Create MarkdownComponentGenerator (IIncrementalGenerator) in src/Markdn.SourceGenerators/MarkdownComponentGenerator.cs
- [x] T023 Create ComponentCodeEmitter in src/Markdn.SourceGenerators/Emitters/ComponentCodeEmitter.cs
- [x] T024 Create RenderTreeBuilderEmitter in src/Markdn.SourceGenerators/Emitters/RenderTreeBuilderEmitter.cs
- [x] T016 [P] Create MarkdigPipelineBuilder in src/Markdn.SourceGenerators/Parsers/MarkdigPipelineBuilder.cs for custom Markdig pipeline configuration
  - **Note**: Markdig created but not usable due to source generator assembly isolation. Using BasicMarkdownParser.cs instead (CommonMark subset: H1-H6, bold, italic, strikethrough, links, code blocks, lists)
- [x] T017 Create RazorSyntaxPreserver implementing IInlineParser in src/Markdn.SourceGenerators/Parsers/RazorSyntaxPreserver.cs for preserving @ expressions and @code blocks
- [x] T018 [P] Create ComponentTagParser implementing IInlineParser in src/Markdn.SourceGenerators/Parsers/ComponentTagParser.cs for preserving <Component /> tags
- [x] T019 Create MarkdownComponentParser orchestrator in src/Markdn.SourceGenerators/Parsers/MarkdownComponentParser.cs that combines YAML, Markdig, and Razor parsing
- [x] T020 Create ComponentNameGenerator in src/Markdn.SourceGenerators/Generators/ComponentNameGenerator.cs for filename to class name conversion
- [x] T021 [P] Create NamespaceGenerator in src/Markdn.SourceGenerators/Generators/NamespaceGenerator.cs for directory structure to namespace mapping
- [x] T022 Create DiagnosticDescriptors in src/Markdn.SourceGenerators/Diagnostics/DiagnosticDescriptors.cs with codes MD001-MD008
- [x] T023 [P] Create ComponentCodeEmitter in src/Markdn.SourceGenerators/CodeGeneration/ComponentCodeEmitter.cs for generating C# source code structure
- [x] T024 Create RenderTreeBuilder code generation in src/Markdn.SourceGenerators/CodeGeneration/RenderTreeBuilderEmitter.cs for BuildRenderTree method
- [x] T025 [P] Create MarkdownComponentGenerator implementing IIncrementalGenerator in src/Markdn.SourceGenerators/MarkdownComponentGenerator.cs with generator registration and pipeline

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Create Simple Markdown Component (Priority: P1) 🎯 MVP

**Goal**: Convert basic Markdown files to usable Blazor components with standard formatting (headings, lists, bold, italic, links)

**Independent Test**: Create `src/Markdn.Blazor.App/Pages/Hello.md` with `# Hello, World!` content, build project, verify `Hello.md.g.cs` is generated, add `<Hello />` to Index.razor, verify it renders correctly

### Implementation for User Story 1

- [x] T026 [US1] Implement AdditionalTextsProvider filtering for .md files in MarkdownComponentGenerator.Initialize method
- [x] T027 [US1] Implement TransformToModel pipeline step in MarkdownComponentGenerator that invokes MarkdownComponentParser
- [x] T028 [US1] Implement basic Markdig to HTML conversion in MarkdigPipelineBuilder (CommonMark extensions only, no Razor preservation yet)
- [x] T029 [US1] Implement ComponentCodeEmitter to generate basic component class structure (namespace, class name, inheritance from ComponentBase)
- [x] T030 [US1] Implement RenderTreeBuilderEmitter to generate BuildRenderTree method with AddMarkupContent calls for static HTML
- [x] T031 [US1] Implement RegisterSourceOutput in MarkdownComponentGenerator to emit .md.g.cs files with auto-generated comment header
- [x] T032 [US1] Create test file src/Markdn.Blazor.App/Pages/Greeting.md with standard Markdown formatting (headings, lists, bold, italic, links)
- [x] T033 [US1] Verify generated src/Markdn.Blazor.App/obj/.../Greeting.md.g.cs contains correct BuildRenderTree with HTML
- [x] T034 [US1] Reference <Greeting /> in src/Markdn.Blazor.App/Pages/Index.razor and verify rendering in browser

**Checkpoint**: ✅ At this point, User Story 1 is fully functional - simple Markdown files generate usable components

---

## Phase 4: User Story 6 - Multi-Rendering Mode Compatibility (Priority: P1)

**Goal**: Ensure generated components work identically in Blazor Server, WebAssembly, and SSR modes

**Independent Test**: Use same Greeting.md component from US1, test in Blazor Server (current), convert project to WebAssembly, test again, convert to SSR render mode, verify identical rendering

**Note**: This is P1 priority and should be verified immediately after US1 before proceeding to other stories

### Implementation for User Story 6

- [x] T035 [US6] Verify generated code uses only ComponentBase and standard RenderTreeBuilder APIs (no server-specific or WASM-specific code)
- [x] T036 [US6] Create src/Markdn.Blazor.App.Wasm/ Blazor WebAssembly test project with same generator reference
- [x] T037 [US6] Copy Greeting.md to WebAssembly project and verify identical rendering
- [x] T038 [US6] Configure Blazor Server project for SSR render mode in Program.cs
- [x] T039 [US6] Verify Greeting component renders identically with SSR render mode
- [x] T040 [US6] Document multi-rendering mode compatibility verification in specs/003-blazor-markdown-components/quickstart.md

**Checkpoint**: ✅ Generated components are proven to work across all three Blazor hosting models

---

## Phase 5: User Story 2 - Create Routable Page from Markdown (Priority: P2)

**Goal**: Generate routable page components by adding YAML front matter with `url` property

**Independent Test**: Create `src/Markdn.Blazor.App/Pages/About.md` with `url: /about` in YAML front matter, build, navigate to /about in browser, verify content displays

### Implementation for User Story 2

- [x] T041 [US2] Implement YAML front matter detection and extraction in YamlFrontMatterParser
- [x] T042 [US2] Implement ComponentMetadata.Url property parsing (scalar string) in YamlFrontMatterParser
- [x] T043 [US2] Implement ComponentMetadata.UrlArray property parsing (sequence) in YamlFrontMatterParser
- [x] T044 [US2] Add validation in YamlFrontMatterParser: Url and UrlArray are mutually exclusive (emit MD008 diagnostic if both specified)
- [x] T045 [US2] Add validation in YamlFrontMatterParser: URLs must start with / (emit MD002 diagnostic if invalid)
- [x] T046 [US2] Implement RouteAttribute generation in ComponentCodeEmitter when Url or UrlArray is present
- [x] T047 [US2] Handle multiple routes by emitting multiple [Route("...")] attributes in ComponentCodeEmitter
- [x] T048 [US2] Create test file src/Markdn.Blazor.App/Pages/About.md with `url: /about` and content
- [x] T049 [US2] Verify generated About.md.g.cs contains [Microsoft.AspNetCore.Components.RouteAttribute("/about")]
- [x] T050 [US2] Navigate to /about in browser and verify content displays
- [x] T051 [US2] Create test file src/Markdn.Blazor.App/Pages/Home.md with `url: [/, /home]` for multiple routes
- [x] T052 [US2] Verify Home component responds to both / and /home routes

**Checkpoint**: Routable pages can be created entirely from Markdown with YAML front matter

---

## Phase 6: User Story 5 - Embed C# Code and Components (Priority: P2)

**Goal**: Support @code blocks, inline expressions, and component references for dynamic content

**Independent Test**: Create Markdown file with `@code { var name = "World"; }` and content `Hello, @name!`, verify it renders "Hello, World!". Add `<Counter />` reference and verify Counter renders.

### Implementation for User Story 5

- [x] T053 [US5] Implement RazorSyntaxPreserver to detect and preserve @ expressions (@identifier, @(expression)) without Markdig escaping
- [x] T054 [US5] Implement @code {} block detection and extraction in RazorSyntaxPreserver
- [x] T055 [US5] Store extracted @code blocks in CodeBlock entities with content and source location
- [x] T056 [US5] Implement ComponentTagParser to detect and preserve <ComponentName /> and <ComponentName>...</ComponentName> syntax
- [x] T057 [US5] Parse component parameters (attribute="value" and attribute="@expression") in ComponentTagParser
- [x] T058 [US5] Register RazorSyntaxPreserver and ComponentTagParser in MarkdigPipelineBuilder custom pipeline (N/A - implemented via `RazorPreserver` and BasicMarkdownParser integration)
- [x] T059 [US5] Implement code block emission in ComponentCodeEmitter after BuildRenderTree method
- [x] T060 [US5] Implement inline expression handling in RenderTreeBuilderEmitter (detect @expression in content, emit AddContent instead of AddMarkupContent)
- [x] T061 [US5] Implement component reference handling in RenderTreeBuilderEmitter (emit OpenComponent, AddAttribute, CloseComponent)
- [x] T062 [US5] Handle component child content with RenderFragment emission in RenderTreeBuilderEmitter
- [x] T063 [US5] Create test file src/Markdn.Blazor.App/Pages/Dynamic.md with @code block and inline expressions
- [x] T064 [US5] Verify generated Dynamic.md.g.cs contains code block members and AddContent calls for expressions
- [x] T065 [US5] Create test file src/Markdn.Blazor.App/Pages/WithComponents.md referencing <Counter /> (built-in Blazor template component)
- [x] T066 [US5] Verify generated WithComponents.md.g.cs contains OpenComponent<Counter> and proper RenderTree calls
- [x] T067 [US5] Test component with parameters: create Alert component and reference it as `<Alert Severity="Warning">Message</Alert>`
- [x] T068 [US5] Verify parameter passing and child content rendering work correctly
- [x] T069 [US5] Add validation for invalid C# syntax in @code blocks (Roslyn will naturally report compilation errors)

**Checkpoint**: ✅ Markdown files can now include dynamic C# code and reusable component references

---

## Phase 7: User Story 4 - Live Hot Reload Support (Priority: P2)

**Goal**: Changes to Markdown files trigger automatic regeneration and browser updates without losing application state

**Independent Test**: Run application in watch mode, edit Dynamic.md from US5, save file, verify browser updates within 3 seconds without manual refresh

### Implementation for User Story 4

- [x] T070 [US4] Verify IIncrementalGenerator pipeline is properly configured for incremental compilation (only changed files regenerate)
- [x] T071 [US4] Create test scenario: run `dotnet watch` in src/Markdn.Blazor.App/
- [x] T072 [US4] Edit src/Markdn.Blazor.App/Pages/Dynamic.md content while app is running
- [x] T073 [US4] Verify source generator reruns automatically and emits new Dynamic.md.g.cs
- [x] T074 [US4] Verify Blazor hot reload picks up the change and updates browser without full reload
- [x] T075 [US4] Verify application state is preserved (e.g., counter values, form inputs remain)
- [x] T076 [US4] Edit YAML front matter (e.g., change page title) and verify hot reload works for metadata changes
- [x] T077 [US4] Document hot reload behavior and limitations in specs/003-blazor-markdown-components/quickstart.md

**Checkpoint**: Development workflow supports live editing with hot reload

---

## Phase 8: User Story 3 - Configure Component Metadata (Priority: P3)

**Goal**: Support all YAML front matter configuration keys: title, $layout, $namespace, $using, $attribute, $parameters, $inherit

**Independent Test**: Create Markdown file with all YAML keys, verify each generates correct directive/attribute in output code

### Implementation for User Story 3

- [x] T078 [US3] Implement ComponentMetadata.Title parsing in YamlFrontMatterParser
- [x] T079 [P] [US3] Implement ComponentMetadata.Namespace parsing in YamlFrontMatterParser with validation (valid C# namespace identifier)
- [x] T080 [P] [US3] Implement ComponentMetadata.Using array parsing in YamlFrontMatterParser with validation
- [x] T081 [P] [US3] Implement ComponentMetadata.Layout parsing in YamlFrontMatterParser with validation (valid C# type identifier)
- [x] T082 [P] [US3] Implement ComponentMetadata.Inherit parsing in YamlFrontMatterParser with validation
- [x] T083 [P] [US3] Implement ComponentMetadata.Attribute array parsing in YamlFrontMatterParser
- [x] T084 [P] [US3] Implement ComponentMetadata.Parameters array parsing with ParameterDefinition (Name, Type) in YamlFrontMatterParser
- [x] T085 [US3] Add parameter name validation (valid C# identifier, emit MD003 diagnostic if invalid)
- [x] T086 [US3] Add parameter type validation (valid C# type syntax, emit MD004 diagnostic if invalid)
- [x] T087 [US3] Add duplicate parameter name detection (emit MD005 diagnostic)
- [x] T088 [US3] Implement <PageTitle> component generation in RenderTreeBuilderEmitter when Title is specified
- [x] T089 [US3] Implement namespace override in ComponentCodeEmitter when $namespace is specified
- [x] T090 [US3] Implement using directive generation in ComponentCodeEmitter when $using is specified
- [x] T091 [US3] Implement LayoutAttribute generation in ComponentCodeEmitter when $layout is specified
- [x] T092 [US3] Implement base class override in ComponentCodeEmitter when $inherit is specified (replace ComponentBase)
- [x] T093 [US3] Implement attribute generation in ComponentCodeEmitter when $attribute is specified
- [x] T094 [US3] Implement parameter property generation in ComponentCodeEmitter when $parameters is specified
- [x] T095 [US3] Decorate parameter properties with [Parameter] attribute in ComponentCodeEmitter
- [x] T096 [US3] Handle nullable reference types for parameter properties (value types not nullable, reference types with = default!)
- [x] T097 [US3] Create comprehensive test file src/Markdn.Blazor.App/Pages/FullMetadata.md with all YAML keys
- [x] T098 [US3] Verify generated FullMetadata.md.g.cs contains all directives, attributes, parameters, and correct structure
- [x] T099 [US3] Create parameterized component test: src/Markdn.Blazor.App/Components/Greeting.md with $parameters
- [x] T100 [US3] Use parameterized Greeting component as `<Greeting Name="Alice" />` and verify parameter binding works
- [x] T101 [US3] Verify parameters declared in YAML are accessible in @code blocks and inline expressions

**Checkpoint**: Full YAML front matter support enables advanced component configuration

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T102 [P] Implement diagnostic reporting for invalid YAML syntax (MD001) in YamlFrontMatterParser
 - [x] T103 [P] Implement diagnostic reporting for malformed Razor syntax (MD007) in RazorSyntaxPreserver
- [x] T104 [P] Add warning diagnostic for unresolvable component references (MD006) in ComponentTagParser
- [x] T105 [P] Add auto-generated file header with generator version and "do not edit" warning in ComponentCodeEmitter
 - [x] T106 [P] Implement sequence number management for RenderTreeBuilder (unique sequential numbers) in RenderTreeBuilderEmitter
 - [x] T107 Add support for date prefix removal in filename (e.g., 2024-11-10-post.md  Post class) in ComponentNameGenerator
 - [x] T108 Add support for kebab-case to PascalCase conversion in ComponentNameGenerator
 - [x] T109 Add support for reserved keyword handling (prefix @) in ComponentNameGenerator
 - [x] T110 Optimize generated code: combine adjacent static HTML into single AddMarkupContent call in RenderTreeBuilderEmitter
 - [x] T111 [P] Document error codes MD001-MD008 in specs/003-blazor-markdown-components/contracts/component-generation-schema.md
 - [x] T112 [P] Update quickstart.md with complete examples for all features
 - [x] T113 [P] Add troubleshooting section to quickstart.md for common issues
- [ ] T114 Create comprehensive example: blog post component with all features in src/Markdn.Blazor.App/Pages/Blog/Post.md
- [ ] T115 Verify all success criteria from spec.md (SC-001 through SC-009)
- [ ] T116 Performance test: create 100+ Markdown files and verify build time is acceptable
- [ ] T117 Run quickstart.md validation walkthrough from start to finish

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **User Story 6 (Phase 4)**: Depends on User Story 1 completion (validates US1 works across rendering modes)
- **User Story 2 (Phase 5)**: Depends on Foundational phase completion - Independent from US1, but sequenced after US6 validation
- **User Story 5 (Phase 6)**: Depends on Foundational phase completion - Independent from US1/US2
- **User Story 4 (Phase 7)**: Depends on User Story 5 completion (needs dynamic content to test hot reload effectively)
- **User Story 3 (Phase 8)**: Depends on US1, US2, US5 completion (builds on all foundational capabilities)
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Foundation only - No dependencies on other stories
- **US6 (P1)**: Depends on US1 completion (validates US1 across platforms)
- **US2 (P2)**: Foundation only - Independent from US1 (but benefits from US1 validation)
- **US5 (P2)**: Foundation only - Independent from US1/US2
- **US4 (P2)**: Depends on US5 (needs dynamic content for meaningful hot reload testing)
- **US3 (P3)**: Depends on US1, US2, US5 (combines all capabilities with metadata)

### Within Each User Story

- Foundation entities and parsers before code generation
- Code emission infrastructure before specific feature implementation
- Core implementation before integration testing
- Story complete before moving to next priority

### Parallel Opportunities

#### Setup Phase (Phase 1)
- T003 (Blazor app creation), T004 (project reference), T005 (test directories) can run in parallel after T001-T002 complete

#### Foundational Phase (Phase 2)
- T007-T014 (all entity models) can run in parallel after T006
- T016, T018, T021, T023 can run in parallel with T015, T017, T019, T022, T024 (different concerns)

#### User Story Parallelization
- After Foundational phase completes:
  - US1 (Phase 3) can proceed
  - After US1 completes and US6 validates → US2 and US5 can proceed in parallel
  - US4 waits for US5
  - US3 waits for US1+US2+US5

#### Polish Phase (Phase 9)
- T102-T105 (diagnostics) can run in parallel
- T111-T113 (documentation) can run in parallel
- T107-T109 (naming improvements) can run in parallel

---

## Parallel Example: User Story 1

```bash
# After Foundational phase completes, start User Story 1:

# Sequential implementation (required order):
Task T026: Filter .md files in generator
Task T027: Invoke parser pipeline
Task T028: Basic Markdig to HTML conversion
Task T029: Generate component class structure
Task T030: Generate BuildRenderTree method
Task T031: Register source output

# Then test in parallel:
Task T032: Create test file Greeting.md
Task T033: Verify generated output (after T032)
Task T034: Test rendering in browser (after T033)
```

---

## Parallel Example: Foundational Phase

```bash
# After T006 completes, launch all entity models in parallel:
Task T007: ComponentMetadata entity
Task T008: ParameterDefinition entity
Task T009: MarkdownContent entity
Task T010: HtmlSegment entity
Task T011: ComponentReference entity
Task T012: ComponentParameter entity
Task T013: CodeBlock entity
Task T014: SourceLocation entity

# While entities are being created, work on parsers (different files):
Task T015: YamlFrontMatterParser
Task T016: MarkdigPipelineBuilder
Task T017: RazorSyntaxPreserver
Task T018: ComponentTagParser
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 6 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (basic Markdown to component)
4. Complete Phase 4: User Story 6 (validate multi-rendering mode)
5. **STOP and VALIDATE**: Test US1+US6 thoroughly, ensure foundation is solid
6. Deploy/demo basic Markdown component generation

**MVP delivers**: Static Markdown content becomes Blazor components that work across all hosting models

### Incremental Delivery

1. **Foundation** (Setup + Foundational) → Generator infrastructure ready
2. **MVP** (US1 + US6) → Static Markdown components work everywhere
3. **Routing** (US2) → Markdown files become pages
4. **Dynamic Content** (US5) → C# code and component embedding
5. **Developer Experience** (US4) → Hot reload support
6. **Advanced Configuration** (US3) → Full YAML metadata support
7. **Polish** → Production-ready with diagnostics and documentation

Each increment adds value without breaking previous functionality.

### Parallel Team Strategy

With multiple developers after Foundational phase:

1. **Developer A**: Complete US1 (basic conversion)
2. **Developer A**: Validate US6 (multi-rendering)
3. After US1+US6 validation:
   - **Developer B**: US2 (routing) - Independent
   - **Developer C**: US5 (dynamic content) - Independent
4. After US5 completes:
   - **Developer D**: US4 (hot reload) - Depends on US5
5. After US1+US2+US5 complete:
   - **Developer E**: US3 (full metadata) - Integrates all
6. All developers: Polish phase tasks in parallel

---

## Notes

- [P] tasks = different files, no dependencies within same phase
- [Story] label (US1, US2, etc.) maps task to specific user story for traceability
- Each user story should be independently completable and testable
- No test tasks included - tests will be written after implementation per standard workflow
- Commit after each task or logical group
- Stop at each checkpoint to validate story independently
- Focus on US1+US6 as true MVP before proceeding to other stories
