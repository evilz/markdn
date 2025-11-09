using FluentAssertions;
using Markdn.Api.Models;

namespace Markdn.Api.Tests.Unit.Models;

/// <summary>
/// Unit tests for ContentIdentifier - T064
/// Tests slug resolution logic (from front-matter or filename fallback)
/// </summary>
public class ContentIdentifierTests
{
    [Theory]
    [InlineData("my-blog-post.md", "my-blog-post")]
    [InlineData("2025-11-09-title.md", "2025-11-09-title")]
    [InlineData("nested/path/article.md", "article")]
    [InlineData("UPPERCASE.MD", "uppercase")]
    [InlineData("with spaces.md", "with-spaces")]
    [InlineData("special!@#chars.md", "specialchars")]
    public void ResolveSlugFromFilename_ShouldExtractAndNormalizeSlug(string filePath, string expectedSlug)
    {
        // Arrange & Act
        var slug = ContentIdentifier.ResolveSlugFromFilename(filePath);

        // Assert
        slug.Should().Be(expectedSlug);
    }

    [Theory]
    [InlineData("my-slug", "my-slug")]
    [InlineData("My Slug", "my-slug")]
    [InlineData("My  Multiple   Spaces", "my-multiple-spaces")]
    [InlineData("Special!@#$%Characters", "specialcharacters")]
    [InlineData("unicode-café-naïve", "unicode-caf-nave")]
    public void NormalizeSlug_ShouldConvertToValidSlugFormat(string input, string expectedSlug)
    {
        // Arrange & Act
        var slug = ContentIdentifier.NormalizeSlug(input);

        // Assert
        slug.Should().Be(expectedSlug);
        slug.Should().MatchRegex("^[a-z0-9-]+$", "slug should only contain lowercase alphanumeric and hyphens");
    }

    [Fact]
    public void GetSlug_WithExplicitSlugInFrontMatter_ShouldReturnFrontMatterSlug()
    {
        // Arrange
        var item = new ContentItem
        {
            Slug = "custom-slug", // Will be set by service
            FilePath = "content/blog/my-post.md",
            CustomFields = new Dictionary<string, object>
            {
                ["slug"] = "custom-slug"
            }
        };

        // Act
        var slug = ContentIdentifier.GetSlug(item);

        // Assert
        slug.Should().Be("custom-slug");
    }

    [Fact]
    public void GetSlug_WithoutSlugInFrontMatter_ShouldFallbackToFilename()
    {
        // Arrange
        var item = new ContentItem
        {
            Slug = "my-post", // Will be set by service from filename
            FilePath = "content/blog/my-post.md",
            CustomFields = new Dictionary<string, object>()
        };

        // Act
        var slug = ContentIdentifier.GetSlug(item);

        // Assert
        slug.Should().Be("my-post");
    }

    [Fact]
    public void GetSlug_WithEmptySlugInFrontMatter_ShouldFallbackToFilename()
    {
        // Arrange
        var item = new ContentItem
        {
            Slug = "my-post", // Will be set by service from filename
            FilePath = "content/blog/my-post.md",
            CustomFields = new Dictionary<string, object>
            {
                ["slug"] = ""
            }
        };

        // Act
        var slug = ContentIdentifier.GetSlug(item);

        // Assert
        slug.Should().Be("my-post");
    }
}
