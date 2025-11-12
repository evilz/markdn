namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Source location for diagnostics and error reporting.
/// </summary>
public sealed class SourceLocation
{
    /// <summary>
    /// Line number (1-based)
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Column number (1-based)
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// File path
    /// </summary>
    public string? FilePath { get; init; }
}
