using FluentAssertions;
using MarkdownToRazorGenerator.Utilities;
using Xunit;

namespace MarkdownToRazorGenerator.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Test-Post", "test-post")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    [InlineData("Special@#$Characters!", "specialcharacters")]
    [InlineData("Underscores_Are_OK", "underscores-are-ok")]
    [InlineData("File.md", "file")]
    [InlineData("UPPERCASE", "uppercase")]
    public void Normalize_WithVariousInputs_GeneratesValidSlug(string input, string expected)
    {
        // Act
        var result = SlugGenerator.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = SlugGenerator.Normalize("");

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("my-slug", "blog", "/blog/my-slug")]
    [InlineData("my-slug", "pages", "/pages/my-slug")]
    [InlineData("my-slug", "other", "/my-slug")]
    [InlineData("/custom/route", "blog", "/custom/route")]
    public void GenerateRoute_WithVariousInputs_GeneratesCorrectRoute(string slug, string directory, string expected)
    {
        // Act
        var result = SlugGenerator.GenerateRoute(slug, directory);

        // Assert
        result.Should().Be(expected);
    }
}
