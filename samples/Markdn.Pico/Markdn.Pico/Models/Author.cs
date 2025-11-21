namespace Markdn.Pico.Models;

public class Author
{
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? Github { get; set; }
    public string? Twitter { get; set; }
}
