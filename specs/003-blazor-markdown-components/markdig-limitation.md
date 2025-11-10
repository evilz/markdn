# Source Generator Assembly Loading Limitation

## Issue

Source generators run in an isolated context and cannot load external assemblies like Markdig at runtime. When attempting to use Markdig in the generator:

```
error MD999: Error generating component: Could not load file or assembly 'Markdig, Version=0.37.0.0'
```

## Root Cause

Source generators:
- Execute in a separate AppDomain/AssemblyLoadContext
- Have restricted assembly loading capabilities
- Cannot access packages referenced by the generator project

## Attempted Solutions

1. **PrivateAssets configuration**: `<PackageReference Include="Markdig" PrivateAssets="all" />`
2. **GeneratePathProperty**: Attempted to get assembly path for manual loading
3. **Analyzer packaging**: Tried packaging Markdig in analyzers/ directory
4. **Dual project references**: Added Markdig to both generator and consumer projects

None of these approaches resolved the assembly loading issue.

## Resolution

Implemented `BasicMarkdownParser.cs` - a custom Markdown parser with no external dependencies supporting:

- ✅ Headings (H1-H6)
- ✅ Bold (**text** or __text__)
- ✅ Italic (*text* or _text_)
- ✅ Strikethrough (~~text~~)
- ✅ Links ([text](url))
- ✅ Inline code (`code`)
- ✅ Code blocks with language syntax (```lang)
- ✅ Lists (unordered with -, *, +)
- ✅ HTML escaping for security

Not yet supported:
- ⏳ Tables
- ⏳ Blockquotes
- ⏳ Horizontal rules
- ⏳ Ordered lists
- ⏳ Task lists

## Future Enhancement Options

### Option A: ILMerge/ILRepack
Merge Markdig.dll into the source generator assembly at build time.

**Pros**: Full Markdig functionality
**Cons**: Complex build configuration, larger generator assembly

### Option B: MSBuild Task Preprocessing
Convert Markdown to HTML in an MSBuild task before source generation.

**Pros**: Can use Markdig freely
**Cons**: Not a true source generator, more complex build pipeline

### Option C: Runtime Loading
Use `Compilation.References` to access Markdig from consumer project assemblies.

**Pros**: Cleaner than ILMerge
**Cons**: Complex implementation, may not work reliably

### Option D: Expand BasicMarkdownParser
Continue enhancing the custom parser with table, blockquote, and ordered list support.

**Pros**: No dependencies, full control
**Cons**: Maintenance burden, may not match full CommonMark spec

## Current Status

BasicMarkdownParser is sufficient for MVP (User Story 1). The system generates working Blazor components with proper formatting for most common Markdown features.

**Files**:
- `src/Markdn.SourceGenerators/Parsers/BasicMarkdownParser.cs` (active)
- `src/Markdn.SourceGenerators/Parsers/MarkdigPipelineBuilder.cs` (preserved for future use)
