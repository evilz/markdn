using System.Collections.Generic;

namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Complete representation of a parsed Markdown file ready for code generation.
/// </summary>
public sealed class MarkdownComponentModel
{
    /// <summary>
    /// Original .md filename (e.g., "About.md")
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Generated C# class name (derived from filename, e.g., "About")
    /// </summary>
    public required string ComponentName { get; init; }

    /// <summary>
    /// Full namespace for generated class (e.g., "MyApp.Pages")
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Parsed YAML front matter metadata (can be empty if no front matter)
    /// </summary>
    public required ComponentMetadata Metadata { get; init; }

    /// <summary>
    /// Parsed Markdown body content
    /// </summary>
    public required MarkdownContent Content { get; init; }

    /// <summary>
    /// Extracted @code {} blocks
    /// </summary>
    public required IReadOnlyList<CodeBlock> CodeBlocks { get; init; }

    /// <summary>
    /// Absolute path to source .md file (for diagnostics)
    /// </summary>
    public required string SourceFilePath { get; init; }
}
