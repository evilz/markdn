# Feature Specification: Markdown to Razor Component Generator

**Feature Branch**: `003-blazor-markdown-components`  
**Created**: 2025-11-10  
**Status**: Draft  
**Input**: User description: "use or implement a Markdown to Razor Component Generator for Blazor. Generated components work seamlessly with all Blazor rendering modes, including Server, WebAssembly, and Server-Side Rendering (SSR). It also fully supports hot reload, ensuring a smooth and productive development experience regardless of your chosen hosting model."

## Clarifications

### Session 2025-11-10

- Q: Should this generator support embedding C# code blocks within Markdown files that execute as Razor code (like MDX)? → A: Support inline C# code blocks (e.g., `@code { }` sections) that can define variables, methods, and logic used in the Markdown content, plus support for including/referencing other Blazor components
- Q: Where should the system place generated files in relation to the source `.md` files? → A: Generate `{filename}.md.g.cs` files using standard C# source generator pattern (e.g., `About.md` generates `About.md.g.cs`), not `.razor` files
- Q: Should generated components support accepting parameters from parent components? → A: Parameters declared in YAML front matter (e.g., `$parameters: [{Name: "Title", Type: "string"}]`) and usable in Markdown/code blocks
- Q: Which Markdown parsing approach should the generator use? → A: Use Markdig with custom pipeline for Razor syntax preservation during parsing (preserving @expressions, @code blocks, and <Component /> tags)
- Q: How should developers add this source generator to their Blazor projects? → A: Part of the project itself, no external NuGet package to install

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Simple Markdown Component (Priority: P1)

A Blazor developer wants to add static content pages (like About Us, Terms of Service) to their application without writing Razor markup. They create a Markdown file with content, and the system automatically generates a usable Razor component.

**Why this priority**: This is the core value proposition - converting Markdown to Razor components. Without this, the feature has no functionality. It represents the minimum viable product that delivers immediate value.

**Independent Test**: Can be fully tested by creating a single Markdown file (e.g., `About.md`) in the project, verifying the corresponding component source file (`About.md.g.cs`) is generated, and using it in another component with `<About />`. Delivers value by allowing static content authoring in Markdown.

**Acceptance Scenarios**:

1. **Given** a Blazor project with the source generator enabled, **When** a developer creates a file `Pages/Greeting.md` with Markdown content `# Hello, World!`, **Then** a `Greeting.md.g.cs` component source file is automatically generated with the rendered HTML
2. **Given** a generated Greeting component exists, **When** a developer uses `<Greeting />` in another component, **Then** the Markdown content renders correctly in the browser
3. **Given** a Markdown file with standard formatting (headings, lists, bold, italic, links), **When** the component is generated, **Then** all Markdown syntax is correctly converted to HTML

---

### User Story 2 - Create Routable Page from Markdown (Priority: P2)

A Blazor developer wants to create navigable pages entirely from Markdown files. They add YAML front matter with a `url` property to their Markdown file, and the system generates a routable page component that responds to navigation.

**Why this priority**: Routing is essential for creating actual pages (not just reusable components). This enables developers to create complete page structures from Markdown, which is a primary use case for content-heavy applications.

**Independent Test**: Can be fully tested by creating a Markdown file with `url: /about` in the front matter, navigating to `/about` in the browser, and verifying the content displays. Delivers value by enabling URL-based page creation without Razor syntax.

**Acceptance Scenarios**:

1. **Given** a Markdown file `Home.md` with front matter `url: /home`, **When** the component is generated, **Then** the generated Razor component includes the equivalent of `@page "/home"`
2. **Given** a routable component for `/home`, **When** a user navigates to `/home` in the browser, **Then** the Markdown content is displayed
3. **Given** a Markdown file with multiple URLs `url: [/, /home]`, **When** the component is generated, **Then** the component responds to both `/` and `/home` routes
4. **Given** a Markdown file without a `url` front matter key, **When** the component is generated, **Then** it is a non-routable component (reusable but not directly accessible via URL)

---

### User Story 3 - Configure Component Metadata (Priority: P3)

A Blazor developer wants to control component behavior using YAML front matter in their Markdown files. They specify properties like `title`, `$layout`, `$namespace`, `$parameters`, and `$using` directives, and the system generates a component with the appropriate Razor directives and parameter properties.

**Why this priority**: This enables advanced component configuration and integration with existing Blazor application structure (layouts, namespaces, dependencies, parameterization). While valuable, it's not required for basic functionality.

**Independent Test**: Can be fully tested by creating a Markdown file with `title: My Page` in the front matter, verifying the generated component includes `<PageTitle>My Page</PageTitle>`, and confirming the title appears in the browser tab. Each YAML key can be tested independently.

**Acceptance Scenarios**:

1. **Given** a Markdown file with `title: About Us`, **When** the component is generated, **Then** it includes `<PageTitle>About Us</PageTitle>`
2. **Given** a Markdown file with `$layout: MainLayout`, **When** the component is generated, **Then** it includes the equivalent of `@layout MainLayout`
3. **Given** a Markdown file with `$namespace: MyApp.CustomPages`, **When** the component is generated, **Then** the generated component is in the `MyApp.CustomPages` namespace
4. **Given** a Markdown file with `$using: [MyApp.Services, MyApp.Models]`, **When** the component is generated, **Then** the component includes the equivalent of `@using MyApp.Services` and `@using MyApp.Models`
5. **Given** a Markdown file with `$attribute: [Authorize(Roles = "Admin")]`, **When** the component is generated, **Then** the component includes the equivalent of `@attribute [Authorize(Roles = "Admin")]`
6. **Given** a Markdown file with `$parameters: [{Name: "Title", Type: "string"}, {Name: "Count", Type: "int"}]`, **When** the component is generated, **Then** it includes `[Parameter] public string Title { get; set; }` and `[Parameter] public int Count { get; set; }` properties
7. **Given** a parameterized component with `Hello, @Title!` in the Markdown, **When** used as `<MyComponent Title="World" />`, **Then** it renders "Hello, World!"
6. **Given** a Markdown file with `$inherit: CustomComponentBase`, **When** the component is generated, **Then** the component includes the equivalent of `@inherits CustomComponentBase`

---

### User Story 4 - Live Hot Reload Support (Priority: P2)

A Blazor developer is actively developing content. They modify a Markdown file while the application is running, and the changes appear in the browser without restarting the application or losing application state.

**Why this priority**: Hot reload is critical for developer productivity and modern development workflows. It significantly improves the development experience and is expected in contemporary Blazor development.

**Independent Test**: Can be fully tested by running the application, editing a Markdown file's content, saving the file, and verifying the browser automatically updates without manual refresh. Delivers value through improved developer experience and faster iteration.

**Acceptance Scenarios**:

1. **Given** a running Blazor application displaying a Markdown-based component, **When** a developer edits the Markdown file and saves, **Then** the browser automatically reflects the changes without requiring a manual refresh
2. **Given** a Blazor application in hot reload mode, **When** a developer changes YAML front matter (e.g., updates the page title), **Then** the component regenerates and the browser updates automatically
3. **Given** application state exists (e.g., form data, in-memory values), **When** a Markdown file is modified and hot reload occurs, **Then** the application state is preserved

---

### User Story 5 - Embed C# Code and Components (Priority: P2)

A Blazor developer wants to create dynamic content pages that mix Markdown with executable C# code and reusable Blazor components. They embed `@code` blocks, inline expressions, and component references directly in their Markdown files to create interactive, data-driven content.

**Why this priority**: This enables MDX-like functionality for Blazor, allowing content authors to create dynamic pages without leaving Markdown. It bridges the gap between static content and interactive components, which is essential for modern content-driven applications.

**Independent Test**: Can be fully tested by creating a Markdown file with an `@code { var name = "World"; }` block and inline expression `Hello, @name!`, verifying the generated component renders "Hello, World!". Component inclusion can be tested by adding `<Counter />` in Markdown and verifying it renders. Delivers value by enabling dynamic, reusable content patterns.

**Acceptance Scenarios**:

1. **Given** a Markdown file with `@code { var greeting = "Hello"; }` and content `@greeting, World!`, **When** the component is generated and rendered, **Then** it displays "Hello, World!"
2. **Given** a Markdown file with inline C# expression `Current time: @DateTime.Now`, **When** the component is rendered, **Then** it displays the current date and time
3. **Given** a Markdown file containing `<Counter />` referencing an existing Blazor component, **When** the component is generated and rendered, **Then** the Counter component appears and functions correctly
4. **Given** a Markdown file with `<Alert Severity="Warning">Message</Alert>` passing parameters to a component, **When** the component is rendered, **Then** the Alert component receives the Severity parameter correctly
5. **Given** a Markdown file with invalid C# syntax in a `@code` block, **When** the component is compiled, **Then** a clear compilation error message is displayed indicating the syntax error location

---

### User Story 6 - Multi-Rendering Mode Compatibility (Priority: P1)

A Blazor developer needs generated components to work across different hosting models. They generate components that function identically whether the application is running in Blazor Server, Blazor WebAssembly, or Server-Side Rendering (SSR) mode.

**Why this priority**: This is a core requirement stated in the feature description. Without this, the feature would be limited to specific Blazor hosting models, reducing its utility and adoption potential.

**Independent Test**: Can be fully tested by creating a single Markdown file, generating the component, and running the same application in each rendering mode (Server, WASM, SSR), verifying identical content display. Delivers value by ensuring portability across Blazor architectures.

**Acceptance Scenarios**:

1. **Given** a generated component from Markdown, **When** deployed in a Blazor Server application, **Then** the content renders correctly
2. **Given** the same generated component, **When** deployed in a Blazor WebAssembly application, **Then** the content renders correctly
3. **Given** the same generated component, **When** deployed in a Blazor application using Server-Side Rendering (SSR), **Then** the content renders correctly
4. **Given** a component using YAML front matter directives, **When** deployed across different rendering modes, **Then** all directives (routing, layout, title) function as expected in each mode

---

### Edge Cases

- What happens when a Markdown file contains invalid YAML front matter (syntax errors)?
- How does the system handle Markdown files with no content (only front matter)?
- What happens when a Markdown file is deleted while the application is running?
- How does the system handle conflicting URL routes (two Markdown files with the same `url`)?
- What happens when a Markdown file references a non-existent layout in `$layout`?
- How does the system handle Markdown files with extremely large content (performance considerations)?
- What happens when YAML front matter contains invalid values for directives (e.g., malformed namespace)?
- How does the system handle special characters or unicode in Markdown filenames?
- What happens when a Markdown file is created in a nested subdirectory structure?
- What happens when embedded C# code in a Markdown file has compilation errors?
- How does the system handle references to non-existent Blazor components within Markdown?
- What happens when a component referenced in Markdown requires parameters but none are provided?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST implement as a C# source generator project within the main solution that automatically detects Markdown files and generates corresponding component source files
- **FR-002**: System MUST be integrated into the Blazor application project via project reference (not external package dependency)
- **FR-003**: System MUST generate `{filename}.md.g.cs` files (e.g., `About.md` → `About.md.g.cs`) following standard source generator naming conventions
- **FR-004**: System MUST use Markdig library (already present in the project) for parsing Markdown content and converting it to HTML
- **FR-005**: System MUST implement custom Markdig pipeline that preserves Razor syntax (@ expressions, @code blocks, component tags) during Markdown parsing
- **FR-006**: System MUST prevent Markdig from escaping or converting Razor/C# syntax elements within Markdown content
- **FR-007**: System MUST support YAML front matter parsing for component metadata configuration
- **FR-008**: System MUST generate routable components when `url` is specified in YAML front matter
- **FR-009**: System MUST support single and multiple URL routes via YAML front matter (scalar and sequence syntax)
- **FR-010**: System MUST generate `<PageTitle>` component when `title` is specified in YAML front matter
- **FR-011**: System MUST generate namespace directive when `$namespace` is specified in YAML front matter
- **FR-012**: System MUST generate using directives when `$using` is specified in YAML front matter (supports sequences)
- **FR-013**: System MUST generate layout directive when `$layout` is specified in YAML front matter
- **FR-014**: System MUST generate inherits directive when `$inherit` is specified in YAML front matter
- **FR-015**: System MUST generate attribute directives when `$attribute` is specified in YAML front matter (supports sequences)
- **FR-016**: System MUST generate component parameter properties when `$parameters` is specified in YAML front matter with Name and Type fields
- **FR-017**: System MUST decorate generated parameter properties with `[Parameter]` attribute
- **FR-018**: Generated parameter properties MUST be accessible in Markdown content and embedded C# code
- **FR-019**: Generated components MUST work identically in Blazor Server, Blazor WebAssembly, and SSR rendering modes
- **FR-020**: System MUST support hot reload - regenerating components when Markdown files are modified during development
- **FR-021**: System MUST preserve application state during hot reload operations
- **FR-022**: System MUST generate component class names based on Markdown filenames (e.g., `Greeting.md` → `Greeting` class)
- **FR-023**: System MUST handle Markdown files in nested directory structures, preserving folder hierarchy in namespaces
- **FR-024**: System MUST provide error messages when YAML front matter contains invalid syntax
- **FR-025**: System MUST handle Markdown files without YAML front matter (generate non-routable components with content only)
- **FR-026**: System MUST support embedding C# code blocks within Markdown using `@code { }` syntax that can define variables, methods, and logic
- **FR-027**: System MUST support inline C# expressions within Markdown content (e.g., `@DateTime.Now`, `@variableName`)
- **FR-028**: System MUST support referencing and rendering other Blazor components within Markdown content using standard Razor component syntax (e.g., `<ComponentName />`)
- **FR-029**: System MUST support passing parameters to referenced Blazor components within Markdown content
- **FR-030**: System MUST provide compilation error messages when embedded C# code contains syntax errors
- **FR-031**: System MUST generate component classes that derive from `ComponentBase` or specified base class
- **FR-032**: Generated source files MUST integrate with the standard Blazor compilation pipeline

### Key Entities *(include if feature involves data)*

- **Markdown File**: Source file with `.md` extension containing optional YAML front matter, Markdown content, embedded C# code blocks, and Blazor component references. Located in the Blazor application project.
- **YAML Front Matter**: Optional metadata block at the beginning of a Markdown file, delimited by `---`, containing key-value pairs that configure the generated component including `url`, `title`, `$namespace`, `$using`, `$layout`, `$inherit`, `$attribute`, and `$parameters`.
- **Generated Component Source**: Output `.md.g.cs` file containing a C# class that inherits from ComponentBase, with BuildRenderTree method rendering the Markdown content as HTML, plus parameter properties, embedded C# code, and component references. Generated by the source generator.
- **Component Parameter**: Property declaration in YAML front matter `$parameters` with Name and Type fields, generating a `[Parameter]` decorated property in the component class that can be passed values from parent components.
- **Component Metadata**: Configuration properties extracted from YAML front matter (url, title, $namespace, $using, $layout, $inherit, $attribute, $parameters) that control generated component behavior.
- **Embedded C# Code**: C# code blocks and expressions within Markdown files that execute as part of the generated component class, enabling dynamic content generation.
- **Source Generator Project**: Separate C# project within the solution implementing IIncrementalGenerator that processes Markdown files during compilation and produces component source code. Referenced by the main Blazor application project.
- **Markdig Pipeline**: Custom Markdig parsing pipeline that converts Markdown to HTML while preserving Razor syntax elements (@ expressions, @code blocks, component tags) without escaping or modification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can create a usable component by adding a Markdown file to the project, with the generated source appearing in IntelliSense within 2 seconds of file creation
- **SC-002**: Generated components render identically across all three Blazor hosting models (Server, WebAssembly, SSR) with 100% functional parity
- **SC-003**: Hot reload updates appear in the browser within 3 seconds of saving a Markdown file modification
- **SC-004**: All standard Markdown syntax (headings, lists, links, emphasis, code blocks, images) renders correctly with 100% accuracy
- **SC-005**: Developers can create routable pages without writing any C# or Razor markup manually, reducing page creation time by at least 60%
- **SC-006**: All 8 supported YAML front matter keys (`url`, `title`, `$namespace`, `$using`, `$layout`, `$inherit`, `$attribute`, `$parameters`) generate correct component code with 100% accuracy
- **SC-007**: The system handles at least 100 Markdown files in a single project without performance degradation during compilation or hot reload
- **SC-008**: 95% of common Markdown content authoring tasks can be completed without consulting documentation
- **SC-009**: Parameterized components can be created and consumed with proper IntelliSense support for parameter names and types
