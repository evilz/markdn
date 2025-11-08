using System.Text.RegularExpressions;

namespace Markdn.Api.Services;

/// <summary>
/// Service for generating slugs from filenames and front-matter
/// </summary>
public partial class SlugGenerator
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}-(.+)\.md$")]
    private static partial Regex DatePrefixPattern();

    [GeneratedRegex(@"[^a-z0-9-]+")]
    private static partial Regex NonSlugCharsPattern();

    public string GenerateSlug(string? frontMatterSlug, string fileName)
    {
        // Priority 1: Use front-matter slug if available
        if (!string.IsNullOrWhiteSpace(frontMatterSlug))
        {
            return Sanitize(frontMatterSlug);
        }

        // Priority 2: Derive from filename
        var name = Path.GetFileNameWithoutExtension(fileName);

        // Remove date prefix pattern (YYYY-MM-DD-)
        var match = DatePrefixPattern().Match(fileName);
        if (match.Success)
        {
            name = match.Groups[1].Value;
        }

        return Sanitize(name);
    }

    private static string Sanitize(string input)
    {
        // Convert to lowercase
        var slug = input.ToLowerInvariant();

        // Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Remove all non-alphanumeric characters except hyphens
        slug = NonSlugCharsPattern().Replace(slug, string.Empty);

        // Remove multiple consecutive hyphens
        slug = Regex.Replace(slug, "-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }
}
