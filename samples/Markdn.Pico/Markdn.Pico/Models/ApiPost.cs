using Markdn.Content;

namespace Markdn.Pico.Models;

/// <summary>
/// Post model with WebAPI generation enabled
/// </summary>
[Collection("Content/ApiPosts/*.md", Name = "ApiPosts", GenerateWebApi = true)]
public class ApiPost
{
    public string Slug { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime PubDate { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
}
