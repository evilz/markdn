using FluentAssertions;
using Markdn.Api.Models;
using Markdn.Api.Services;
using Xunit;

namespace Markdn.Api.Tests.Unit.Services;

public class FrontMatterParserTests
{
    [Fact]
    public async Task ParseAsync_WithValidYaml_ShouldReturnParsedFrontMatter()
    {
        // Arrange
        var yaml = @"---
title: Test Post
date: 2025-11-08
author: John Doe
tags: [test, unit]
category: testing
---";
        var parser = new FrontMatterParser();

        // Act
        var result = await parser.ParseAsync(yaml, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Post");
        result.Date.Should().Be("2025-11-08");
        result.Author.Should().Be("John Doe");
        result.Tags.Should().HaveCount(2);
        result.Category.Should().Be("testing");
    }

    [Fact]
    public async Task ParseAsync_WithInvalidYaml_ShouldReturnEmptyFrontMatter()
    {
        // Arrange
        var yaml = @"---
title: Test Post
invalid yaml: [unclosed
---";
        var parser = new FrontMatterParser();

        // Act
        var result = await parser.ParseAsync(yaml, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_WithMissingFrontMatter_ShouldReturnEmptyFrontMatter()
    {
        // Arrange
        var content = "# Just Markdown Content";
        var parser = new FrontMatterParser();

        // Act
        var result = await parser.ParseAsync(content, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().BeNull();
    }
}
