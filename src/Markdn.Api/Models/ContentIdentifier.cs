namespace Markdn.Api.Models;

/// <summary>
/// Encapsulates the logic for determining content item identifiers.
/// </summary>
public class ContentIdentifier
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
}
