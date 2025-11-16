using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownToRazorGenerator.Utilities;

/// <summary>
/// Utilities for generating slugs and routes
/// </summary>
public static class SlugGenerator
{
    /// <summary>
    /// Normalizes a string to a URL-friendly slug
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remove file extension if present
        if (input.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring(0, input.Length - 3);
        }

        // Convert to lowercase
        var slug = input.ToLowerInvariant();

        // Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Remove invalid characters (keep only alphanumeric and hyphens)
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    /// <summary>
    /// Generates a route from a slug and directory context
    /// </summary>
    public static string GenerateRoute(string slug, string directory)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "/";
        }

        var normalizedSlug = slug.StartsWith("/") ? slug : "/" + slug;

        // If directory indicates blog or pages, prefix appropriately
        var dirName = Path.GetFileName(directory)?.ToLowerInvariant();
        
        if (dirName == "blog" && !normalizedSlug.StartsWith("/blog/"))
        {
            return $"/blog{normalizedSlug}";
        }
        else if (dirName == "pages" && !normalizedSlug.StartsWith("/pages/"))
        {
            return $"/pages{normalizedSlug}";
        }

        return normalizedSlug;
    }
}
