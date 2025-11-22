namespace Markdn.Pico.Models;

public class SiteConfig
{
    public string Slug { get; set; } = default!;
    public string SiteName { get; set; } = default!;
    public string? Theme { get; set; }
    public bool EnableComments { get; set; }
    public int MaxPostsPerPage { get; set; }
}
