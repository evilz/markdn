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

    [Fact]
    public void ToHtmlWithSections_WithNoSections_ReturnsOnlyMainContent()
    {
        // Arrange
        var markdown = "# Main Content\n\nThis is the main page content.";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        mainContent.Should().Contain("<h1");
        mainContent.Should().Contain("Main Content");
        mainContent.Should().Contain("main page content");
        sections.Should().BeEmpty();
    }

    [Fact]
    public void ToHtmlWithSections_WithSingleSection_ExtractsSection()
    {
        // Arrange
        var markdown = @"<Section Name=""header"">
# Page Header
This is header content
</Section>

Main content here";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections.Should().HaveCount(1);
        sections[0].Name.Should().Be("header");
        sections[0].Content.Should().Contain("<h1");
        sections[0].Content.Should().Contain("Page Header");
        mainContent.Should().Contain("Main content here");
        mainContent.Should().NotContain("Section Name");
    }

    [Fact]
    public void ToHtmlWithSections_WithMultipleSections_ExtractsAllSections()
    {
        // Arrange
        var markdown = @"<Section Name=""header"">
# Page Header
</Section>

<Section Name=""sidebar"">
- Link 1
- Link 2
</Section>

Main content here";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections.Should().HaveCount(2);
        sections[0].Name.Should().Be("header");
        sections[0].Content.Should().Contain("Page Header");
        sections[1].Name.Should().Be("sidebar");
        sections[1].Content.Should().Contain("<li>Link 1</li>");
        mainContent.Should().Contain("Main content here");
    }

    [Fact]
    public void ToHtmlWithSections_WithSectionInCodeBlock_IgnoresSectionTag()
    {
        // Arrange
        var markdown = @"Here's how to use sections:

```markdown
<Section Name=""example"">
Content here
</Section>
```

<Section Name=""realSection"">
Actual section content
</Section>

More content";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections.Should().HaveCount(1);
        sections[0].Name.Should().Be("realSection");
        mainContent.Should().Contain("Here's how to use sections");
        mainContent.Should().Contain("<code");
        mainContent.Should().Contain("More content");
    }

    [Fact]
    public void ToHtmlWithSections_WithSectionInInlineCode_IgnoresSectionTag()
    {
        // Arrange
        var markdown = @"Use `<Section Name=""test"">` to define sections.

<Section Name=""actual"">
Real section
</Section>";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections.Should().HaveCount(1);
        sections[0].Name.Should().Be("actual");
        mainContent.Should().Contain("<code>&lt;Section Name");
    }

    [Fact]
    public void ToHtmlWithSections_WithMarkdownInSection_ProcessesMarkdown()
    {
        // Arrange
        var markdown = @"<Section Name=""sidebar"">
## Sidebar Title
- **Bold item**
- *Italic item*
- [Link](http://example.com)
</Section>

Main content";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections[0].Content.Should().Contain("<h2");
        sections[0].Content.Should().Contain("<strong>Bold item</strong>");
        sections[0].Content.Should().Contain("<em>Italic item</em>");
        sections[0].Content.Should().Contain("<a href=\"http://example.com\">");
    }

    [Fact]
    public void ToHtmlWithSections_WithSingleQuotesInAttribute_WorksCorrectly()
    {
        // Arrange
        var markdown = @"<Section Name='mysection'>
Content
</Section>";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections.Should().HaveCount(1);
        sections[0].Name.Should().Be("mysection");
    }

    [Fact]
    public void ToHtmlWithSections_WithCaseInsensitiveTags_WorksCorrectly()
    {
        // Arrange
        var markdown = @"<section Name=""test"">
Content
</SECTION>";

        // Act
        var (mainContent, sections) = _converter.ToHtmlWithSections(markdown);

        // Assert
        sections.Should().HaveCount(1);
        sections[0].Name.Should().Be("test");
    }
}
