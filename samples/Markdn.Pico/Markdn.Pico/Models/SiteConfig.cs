using Markdn.Content;

namespace Markdn.Pico.Models;

/// <summary>
/// Configuration model to test file format support
/// Note: Currently uses .md files with YAML front-matter
/// Pure YAML/JSON/TOML parsing is planned for future release
/// </summary>
[Collection("Content/Config/*.md", Name = "SiteConfig")]
public class SiteConfig
{
    public string Slug { get; set; } = default!;
    public string SiteName { get; set; } = default!;
    public string? Theme { get; set; }
    public bool EnableComments { get; set; }
    public int MaxPostsPerPage { get; set; }
}
