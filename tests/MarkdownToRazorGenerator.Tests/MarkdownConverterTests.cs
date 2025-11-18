using FluentAssertions;
using MarkdownToRazorGenerator.Parsers;
using Xunit;

namespace MarkdownToRazorGenerator.Tests;

public class MarkdownConverterTests
{
    private readonly MarkdownConverter _converter;

    public MarkdownConverterTests()
    {
        _converter = new MarkdownConverter();
    }

    [Fact]
    public void ToHtml_WithBasicMarkdown_ConvertsCorrectly()
    {
        // Arrange
        var markdown = "# Heading\n\nSome **bold** text.";

        // Act
        var html = _converter.ToHtml(markdown);

        // Assert
        html.Should().Contain("<h1");
        html.Should().Contain("Heading");
        html.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void ToHtml_WithList_ConvertsToHtmlList()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var html = _converter.ToHtml(markdown);

        // Assert
        html.Should().Contain("<ul>");
        html.Should().Contain("<li>Item 1</li>");
        html.Should().Contain("<li>Item 2</li>");
        html.Should().Contain("<li>Item 3</li>");
    }

    [Fact]
    public void ToHtml_WithCodeBlock_ConvertsToPreCode()
    {
        // Arrange
        var markdown = "```csharp\nvar x = 10;\n```";

        // Act
        var html = _converter.ToHtml(markdown);

        // Assert
        html.Should().Contain("<pre>");
        html.Should().Contain("<code");
        html.Should().Contain("var x = 10;");
    }

    [Fact]
    public void ToHtml_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var html = _converter.ToHtml("");

        // Assert
        html.Should().BeEmpty();
    }

    [Fact]
    public void ToHtml_WithRazorExpression_PreservesRazorSyntax()
    {
        // Arrange
        var markdown = "Greeting generated at: @DateTime.Now.ToString(\"HH:mm:ss\")";

        // Act
        var html = _converter.ToHtml(markdown);

        // Assert
        html.Should().Contain("@DateTime.Now.ToString(\"HH:mm:ss\")");
        html.Should().NotContain("&quot;");
    }

    [Fact]
    public void ToHtml_WithMultipleRazorExpressions_PreservesAll()
    {
        // Arrange
        var markdown = "Hello @Model.Name, the time is @DateTime.Now.ToString(\"HH:mm\")";

        // Act
        var html = _converter.ToHtml(markdown);

        // Assert
        html.Should().Contain("@Model.Name");
        html.Should().Contain("@DateTime.Now.ToString(\"HH:mm\")");
        html.Should().NotContain("&quot;");
    }
}
