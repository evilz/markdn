using FluentAssertions;
using Markdn.Api.Services;
using Xunit;

namespace Markdn.Api.Tests.Unit.Services;

public class MarkdownParserTests
{
    [Fact]
    public async Task ParseAsync_WithValidMarkdown_ShouldReturnHtml()
    {
        // Arrange
        var markdown = "# Heading\n\nParagraph text";
        var parser = new MarkdownParser();

        // Act
        var result = await parser.ParseAsync(markdown, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<h1");
        result.Should().Contain("Heading");
        result.Should().Contain("<p>");
    }

    [Fact]
    public async Task ParseAsync_WithGfmFeatures_ShouldRenderCorrectly()
    {
        // Arrange
        var markdown = @"| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |

- [x] Task 1
- [ ] Task 2";
        var parser = new MarkdownParser();

        // Act
        var result = await parser.ParseAsync(markdown, CancellationToken.None);

        // Assert
        result.Should().Contain("<table>");
        result.Should().Contain("<th>");
        result.Should().Contain("type=\"checkbox\"");
        result.Should().Contain("checked=\"checked\"");
    }
}
