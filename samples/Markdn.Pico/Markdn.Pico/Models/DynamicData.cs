using Markdn.Content;

namespace Markdn.Pico.Models;

/// <summary>
/// Dynamic model to test flexible content with structured data
/// Note: Currently uses .md files with YAML front-matter
/// C# dynamic support for pure JSON/YAML is planned for future release
/// </summary>
[Collection("Content/Dynamic/*.md", Name = "DynamicData")]
public class DynamicData
{
    public string Slug { get; set; } = default!;
    public string? ProductName { get; set; }
    public double? Price { get; set; }
    public string? Category { get; set; }
    public bool InStock { get; set; }
}
