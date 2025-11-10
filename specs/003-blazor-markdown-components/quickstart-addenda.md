## Addenda: componentNamespaces and MD006 (explicit guidance)

This short addenda explains the `componentNamespaces` front-matter key and the MD006 diagnostic introduced by the generator to help with component resolution issues.

When a Markdown file references other Blazor components (for example `<Counter />`, `<Alert />`, or project components), the generator attempts to resolve the component type names using the current compilation symbols. In some cases (cross-generator ordering, Razor compilation timing) the generator cannot reliably find the type's namespace and will emit a warning diagnostic MD006.

Use one of the following patterns to make component resolution deterministic:

1) Use `$using` in YAML front matter (recommended when you want the effect of a using directive):

```yaml
---
$using:
  - MyApp.Components
  - MyApp.Shared
---
```

2) (Alternative) Use `componentNamespaces` explicit list (generator-specific):

```yaml
---
componentNamespaces:
  - MyApp.Components
  - MyApp.Shared
---
```

Notes:
- `$using` follows the contract in `component-generation-schema.md` and emits `using` directives in the generated file.
- `componentNamespaces` is an explicit metadata override accepted by the generator as a robust fallback; it will also result in emitted `using` directives.
- If neither helps, ensure the referenced component types are present in the same project or available via project references and that build ordering does not block symbol visibility.

Example: Markdown file that needs explicit namespaces

```markdown
---
url: /with-components
title: With Components
componentNamespaces:
  - MyApp.Components
---

# Page with components

<Counter />
<Alert Severity="Warning">Be careful</Alert>
```

This will produce a generated `.md.g.cs` file with the appropriate `using MyApp.Components;` directive and `OpenComponent<Counter>` emission.

If you see MD006, add either `$using` or `componentNamespaces` to your front matter and rebuild.

If you want, open an issue in the repository describing the specific component and project layout — we can investigate generator heuristics for improved auto-resolution in future releases.
