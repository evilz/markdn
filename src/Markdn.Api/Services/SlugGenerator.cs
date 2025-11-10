using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Markdn.Api.Services;

/// <summary>
/// Service for generating URL-safe slugs from filenames and front-matter with precedence logic.
/// Handles international characters by removing diacritics (café → cafe, São Paulo → sao-paulo).
/// </summary>
public partial class SlugGenerator
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}-(.+)\.md$")]
    private static partial Regex DatePrefixPattern();

    [GeneratedRegex(@"[^a-z0-9-]+")]
    private static partial Regex NonSlugCharsPattern();

    /// <summary>
    /// Generates a URL-safe slug with precedence: front-matter slug > filename
    /// </summary>
    /// <param name="frontMatterSlug">Optional slug from front-matter</param>
    /// <param name="fileName">Filename to derive slug from if front-matter slug not provided</param>
    /// <returns>Sanitized URL-safe slug</returns>
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
        // Step 1: Normalize to FormD (decomposed form) to separate base chars from diacritics
        var normalized = input.Normalize(NormalizationForm.FormD);

        // Step 2: Remove diacritical marks (café → cafe, München → munchen)
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // Step 3: Convert to lowercase
        var slug = sb.ToString().ToLowerInvariant();

        // Step 4: Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Step 5: Remove all non-alphanumeric characters except hyphens
        slug = NonSlugCharsPattern().Replace(slug, string.Empty);

        // Step 6: Remove multiple consecutive hyphens
        slug = Regex.Replace(slug, "-+", "-");

        // Step 7: Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }
}
