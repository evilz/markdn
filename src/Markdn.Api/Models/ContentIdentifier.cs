using System.Text.RegularExpressions;

namespace Markdn.Api.Models;

/// <summary>
/// Encapsulates the logic for determining content item identifiers.
/// </summary>
public partial class ContentIdentifier
{
    /// <summary>
    /// Gets or sets the explicit slug from front-matter (if provided).
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Gets or sets the filename without extension (used as fallback).
    /// </summary>
    public required string Filename { get; set; }

    /// <summary>
    /// Gets the final resolved identifier (Slug ?? Filename).
    /// Normalizes to lowercase and replaces spaces with hyphens.
    /// </summary>
    public string ResolvedId =>
        (Slug ?? Filename)
            .ToLowerInvariant()
            .Replace(" ", "-");

    /// <summary>
    /// Resolves a slug from a content item, using explicit slug or filename fallback.
    /// </summary>
    /// <param name="item">The content item.</param>
    /// <returns>The resolved slug.</returns>
    public static string GetSlug(ContentItem item)
    {
        // Try to get explicit slug from front-matter
        if (item.CustomFields.TryGetValue("slug", out var slugValue) && slugValue != null)
        {
            var slug = slugValue.ToString();
            if (!string.IsNullOrWhiteSpace(slug))
            {
                return NormalizeSlug(slug);
            }
        }

        // Fallback to filename
        return ResolveSlugFromFilename(item.FilePath);
    }

    /// <summary>
    /// Extracts and normalizes a slug from a file path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The normalized slug.</returns>
    public static string ResolveSlugFromFilename(string filePath)
    {
        var filename = Path.GetFileNameWithoutExtension(filePath);
        return NormalizeSlug(filename);
    }

    /// <summary>
    /// Normalizes a string to a valid slug format (lowercase alphanumeric with hyphens).
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The normalized slug.</returns>
    public static string NormalizeSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Convert to lowercase
        var slug = input.ToLowerInvariant();

        // Replace spaces and multiple spaces with single hyphen
        slug = MultipleSpacesRegex().Replace(slug, "-");
        slug = slug.Replace(' ', '-');

        // Remove special characters (keep only alphanumeric and hyphens)
        slug = NonAlphanumericRegex().Replace(slug, "");

        // Remove leading/trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex NonAlphanumericRegex();
}
