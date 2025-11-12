namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Represents an @code {} block extracted from Markdown.
/// </summary>
public sealed class CodeBlock
{
    /// <summary>
    /// C# code content (without @code {} wrapper)
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Source location for diagnostics
    /// </summary>
    public SourceLocation? Location { get; init; }
}
