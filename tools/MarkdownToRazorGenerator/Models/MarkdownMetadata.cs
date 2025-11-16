namespace MarkdownToRazorGenerator.Models;

/// <summary>
/// Represents metadata extracted from YAML front-matter
/// </summary>
public class MarkdownMetadata
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Route { get; set; }
    public string? Layout { get; set; }
    public string? Summary { get; set; }
    public DateTime? Date { get; set; }
    public List<string>? Tags { get; set; }
}
