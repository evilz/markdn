public class Post
{
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime PubDate { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Route { get; set; }
    public List<string> Tags { get; set; } = new();
}
