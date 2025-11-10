namespace Markdn.SourceGenerators.Models;

/// <summary>
/// Specification for a component parameter property.
/// </summary>
public sealed class ParameterDefinition
{
    /// <summary>
    /// Parameter property name (e.g., "PostId", "Title")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// C# type name (e.g., "string", "int", "List&lt;User&gt;")
    /// </summary>
    public required string Type { get; init; }
}
