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

    [Fact]
    public void Parse_WithVariablesAndParameters_PreservesTypes()
    {
        // Arrange
        var content = @"---
title: Test Page
variables:
  count: 42
  price: 19.99
  isActive: true
  name: Test String
parameters:
  pageSize: 10
  enabled: false
  factor: 2.5
---

# Content Here";

        // Act
        var (metadata, body, errors) = _parser.Parse(content);

        // Assert
        errors.Should().BeEmpty();
        metadata.Title.Should().Be("Test Page");
        
        // Verify variables are present and types are preserved
        metadata.Variables.Should().NotBeNull();
        metadata.Variables.Should().HaveCount(4);
        // YamlDotNet may deserialize small integers as byte/short, so check for numeric types
        metadata.Variables!["count"].Should().BeAssignableTo<IConvertible>().Which.ToInt32(null).Should().Be(42);
        metadata.Variables["price"].Should().BeAssignableTo<IConvertible>().Which.ToDouble(null).Should().BeApproximately(19.99, 0.001);
        metadata.Variables["isActive"].Should().BeOfType<bool>().And.Be(true);
        metadata.Variables["name"].Should().BeOfType<string>().And.Be("Test String");
        
        // Verify parameters are present and types are preserved
        metadata.Parameters.Should().NotBeNull();
        metadata.Parameters.Should().HaveCount(3);
        metadata.Parameters!["pageSize"].Should().BeAssignableTo<IConvertible>().Which.ToInt32(null).Should().Be(10);
        metadata.Parameters["enabled"].Should().BeOfType<bool>().And.Be(false);
        metadata.Parameters["factor"].Should().BeAssignableTo<IConvertible>().Which.ToDouble(null).Should().BeApproximately(2.5, 0.001);
    }
}
