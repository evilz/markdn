using FluentAssertions;
using MarkdownToRazorGenerator.Parsers;
using Xunit;

namespace MarkdownToRazorGenerator.Tests;

public class FrontMatterParserTests
{
    private readonly FrontMatterParser _parser;

    public FrontMatterParserTests()
    {
        _parser = new FrontMatterParser();
    }

    [Fact]
    public void Parse_WithValidFrontMatter_ExtractsMetadata()
    {
        // Arrange
        var content = @"---
title: Test Title
slug: test-slug
date: 2025-11-15
tags:
  - tag1
  - tag2
---

# Content Here";

        // Act
        var (metadata, body, errors) = _parser.Parse(content);

        // Assert
        errors.Should().BeEmpty();
        metadata.Title.Should().Be("Test Title");
        metadata.Slug.Should().Be("test-slug");
        metadata.Date.Should().Be(new DateTime(2025, 11, 15));
        metadata.Tags.Should().HaveCount(2);
        metadata.Tags.Should().Contain(new[] { "tag1", "tag2" });
        body.Should().Contain("# Content Here");
    }

    [Fact]
    public void Parse_WithoutFrontMatter_ReturnsEmptyMetadata()
    {
        // Arrange
        var content = "# Just Markdown\n\nNo front matter here.";

        // Act
        var (metadata, body, errors) = _parser.Parse(content);

        // Assert
        errors.Should().BeEmpty();
        metadata.Title.Should().BeNull();
        metadata.Slug.Should().BeNull();
        body.Should().Be(content);
    }

    [Fact]
    public void Parse_WithInvalidYaml_ReturnsError()
    {
        // Arrange
        var content = @"---
title: Test
invalid yaml here: [[[
---

# Content";

        // Act
        var (metadata, body, errors) = _parser.Parse(content);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("YAML parsing error"));
    }

    [Fact]
    public void ExtractFirstH1_WithH1Present_ReturnsHeading()
    {
        // Arrange
        var markdown = "Some text\n\n# My Heading\n\nMore content";

        // Act
        var result = _parser.ExtractFirstH1(markdown);

        // Assert
        result.Should().Be("My Heading");
    }

    [Fact]
    public void ExtractFirstH1_WithNoH1_ReturnsNull()
    {
        // Arrange
        var markdown = "## H2 Only\n\nSome content";

        // Act
        var result = _parser.ExtractFirstH1(markdown);

        // Assert
        result.Should().BeNull();
    }
}
