using Markdn.Content;

namespace Markdn.Pico.Models;

/// <summary>
/// Author model to test file format support
/// Note: Currently uses .md files with YAML front-matter
/// Pure YAML/JSON/TOML parsing is planned for future release
/// </summary>
[Collection("Content/Authors/*.md", Name = "Authors")]
public class Author
{
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? Github { get; set; }
    public string? Twitter { get; set; }
}
