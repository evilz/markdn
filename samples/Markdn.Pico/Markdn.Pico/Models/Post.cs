namespace Markdn.Pico.Models;

public class Post
{
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime PubDate { get; set; }
    public string? Description { get; set; }
}

