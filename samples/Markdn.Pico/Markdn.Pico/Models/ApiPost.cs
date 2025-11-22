namespace Markdn.Pico.Models;

public class ApiPost
{
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime PubDate { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
}
