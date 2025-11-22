namespace Markdn.Pico.Models;

public class DynamicData
{
    public string Slug { get; set; } = default!;
    public string? ProductName { get; set; }
    public double? Price { get; set; }
    public string? Category { get; set; }
    public bool InStock { get; set; }
}
