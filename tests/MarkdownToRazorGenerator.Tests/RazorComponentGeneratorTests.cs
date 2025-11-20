using FluentAssertions;
using MarkdownToRazorGenerator.Generators;
using MarkdownToRazorGenerator.Models;
using Xunit;

namespace MarkdownToRazorGenerator.Tests;

public class RazorComponentGeneratorTests
{
    private readonly RazorComponentGenerator _generator;

    public RazorComponentGeneratorTests()
    {
        _generator = new RazorComponentGenerator();
    }

    [Fact]
    public void Generate_WithBasicMetadata_IncludesPageMetadataClass()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page",
            Slug = "test-page"
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("private class PageMetadata");
        result.Should().Contain("public string? Title { get; set; }");
        result.Should().Contain("public DateTime? Date { get; set; }");
        result.Should().Contain("public List<string>? Tags { get; set; }");
    }

    [Fact]
    public void Generate_WithMetadata_InitializesPageMetadataInstance()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page",
            Slug = "test-page",
            Summary = "A test page"
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("private readonly PageMetadata _pageMetadata = new()");
        result.Should().Contain("Title = \"Test Page\"");
        result.Should().Contain("Slug = \"test-page\"");
        result.Should().Contain("Summary = \"A test page\"");
    }

    [Fact]
    public void Generate_WithTags_IncludesTagsInMetadata()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page",
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("Tags = new List<string> { \"tag1\", \"tag2\", \"tag3\" }");
    }

    [Fact]
    public void Generate_WithDate_IncludesDateInMetadata()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page",
            Date = new DateTime(2025, 11, 20)
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("Date = DateTime.Parse(\"2025-11-20T00:00:00.0000000\")");
    }

    [Fact]
    public void Generate_WithCascadingValue_WrapsContentInCascadingValue()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page"
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("<CascadingValue Value=\"@_pageMetadata\">");
        result.Should().Contain("</CascadingValue>");
        // Content should be inside the CascadingValue
        var cascadingStart = result.IndexOf("<CascadingValue");
        var cascadingEnd = result.IndexOf("</CascadingValue>");
        var contentPosition = result.IndexOf("<p>Test content</p>");
        contentPosition.Should().BeGreaterThan(cascadingStart);
        contentPosition.Should().BeLessThan(cascadingEnd);
    }

    [Fact]
    public void Generate_WithLayout_IncludesLayoutDirective()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page",
            Layout = "MyApp.Layouts.CustomLayout"
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("@layout MyApp.Layouts.CustomLayout");
    }

    [Fact]
    public void Generate_WithAllMetadata_IncludesAllFields()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Complete Test",
            Slug = "complete-test",
            Route = "/complete",
            Summary = "A complete test page",
            Date = new DateTime(2025, 11, 20),
            Tags = new List<string> { "test", "complete" }
        };
        var htmlContent = "<h1>Content</h1>";
        var route = "/complete";
        var title = "Complete Test";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().Contain("Title = \"Complete Test\"");
        result.Should().Contain("Slug = \"complete-test\"");
        result.Should().Contain("Route = \"/complete\"");
        result.Should().Contain("Summary = \"A complete test page\"");
        result.Should().Contain("Date = DateTime.Parse(\"2025-11-20T00:00:00.0000000\")");
        result.Should().Contain("Tags = new List<string> { \"test\", \"complete\" }");
    }

    [Fact]
    public void Generate_WithNoTags_DoesNotIncludeTagsProperty()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page"
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().NotContain("Tags = ");
    }

    [Fact]
    public void Generate_WithEmptyTags_DoesNotIncludeTagsProperty()
    {
        // Arrange
        var metadata = new MarkdownMetadata
        {
            Title = "Test Page",
            Tags = new List<string>()
        };
        var htmlContent = "<p>Test content</p>";
        var route = "/test";
        var title = "Test Page";

        // Act
        var result = _generator.Generate(metadata, htmlContent, route, title);

        // Assert
        result.Should().NotContain("Tags = ");
    }
}
