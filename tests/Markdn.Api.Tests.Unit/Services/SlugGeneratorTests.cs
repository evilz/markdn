using FluentAssertions;
using Markdn.Api.Services;
using Xunit;

namespace Markdn.Api.Tests.Unit.Services;

public class SlugGeneratorTests
{
    [Fact]
    public void GenerateSlug_WithFrontMatterSlug_ShouldUseFrontMatterSlug()
    {
        // Arrange
        var generator = new SlugGenerator();
        var frontMatterSlug = "custom-slug";
        var fileName = "2024-01-15-different-title.md";

        // Act
        var result = generator.GenerateSlug(frontMatterSlug, fileName);

        // Assert
        result.Should().Be("custom-slug");
    }

    [Fact]
    public void GenerateSlug_WithoutFrontMatterSlug_ShouldDeriveFromFileName()
    {
        // Arrange
        var generator = new SlugGenerator();
        var fileName = "2024-01-15-my-blog-post.md";

        // Act
        var result = generator.GenerateSlug(null, fileName);

        // Assert
        result.Should().Be("my-blog-post");
    }

    [Fact]
    public void GenerateSlug_WithSpecialCharacters_ShouldSanitize()
    {
        // Arrange
        var generator = new SlugGenerator();
        var fileName = "My Post! With Special@Characters#.md";

        // Act
        var result = generator.GenerateSlug(null, fileName);

        // Assert
        result.Should().MatchRegex("^[a-z0-9-]+$");
        result.Should().NotContain(" ");
        result.Should().NotContain("!");
        result.Should().NotContain("@");
    }

    [Theory]
    [InlineData("café", "cafe")]
    [InlineData("naïve", "naive")]
    [InlineData("München", "munchen")]
    [InlineData("São Paulo", "sao-paulo")]
    [InlineData("Zürich", "zurich")]
    [InlineData("exposé", "expose")]
    public void GenerateSlug_WithInternationalCharacters_ShouldRemoveDiacritics(string input, string expected)
    {
        // Arrange
        var generator = new SlugGenerator();

        // Act
        var result = generator.GenerateSlug(input, "fallback.md");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateSlug_WithMixedInternationalAndSpecialChars_ShouldNormalizeCorrectly()
    {
        // Arrange
        var generator = new SlugGenerator();
        var input = "Café & Restaurant: Crème Brûlée!";

        // Act
        var result = generator.GenerateSlug(input, "fallback.md");

        // Assert
        result.Should().Be("cafe-restaurant-creme-brulee");
    }
}
