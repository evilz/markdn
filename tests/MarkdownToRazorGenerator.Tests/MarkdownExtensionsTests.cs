using FluentAssertions;
using MarkdownToRazorGenerator.Extensions;
using MarkdownToRazorGenerator.Models;
using Xunit;

namespace MarkdownToRazorGenerator.Tests;

public class MarkdownExtensionsTests
{
    [Fact]
    public void GetFrontMatter_WithValidFrontMatter_ReturnsMetadata()
    {
        // Arrange
        var markdown = @"---
title: Test Title
slug: test-slug
date: 2025-11-15
tags:
  - tag1
  - tag2
---

# Content Here";

        // Act
        var metadata = markdown.GetFrontMatter<MarkdownMetadata>();

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Title.Should().Be("Test Title");
        metadata.Slug.Should().Be("test-slug");
        metadata.Date.Should().Be(new DateTime(2025, 11, 15));
        metadata.Tags.Should().HaveCount(2);
        metadata.Tags.Should().Contain(new[] { "tag1", "tag2" });
    }

    [Fact]
    public void GetFrontMatter_WithoutFrontMatter_ReturnsNull()
    {
        // Arrange
        var markdown = "# Just Markdown\n\nNo front matter here.";

        // Act
        var metadata = markdown.GetFrontMatter<MarkdownMetadata>();

        // Assert
        metadata.Should().BeNull();
    }

    [Fact]
    public void GetMarkdownBody_WithFrontMatter_ReturnsBodyOnly()
    {
        // Arrange
        var markdown = @"---
title: Test Title
---

# Content Here
Some text";

        // Act
        var body = markdown.GetMarkdownBody();

        // Assert
        body.Should().NotBeNull();
        body.Should().Contain("# Content Here");
        body.Should().Contain("Some text");
        body.Should().NotContain("---");
        body.Should().NotContain("title:");
    }

    [Fact]
    public void GetMarkdownBody_WithoutFrontMatter_ReturnsFullContent()
    {
        // Arrange
        var markdown = "# Just Markdown\n\nNo front matter here.";

        // Act
        var body = markdown.GetMarkdownBody();

        // Assert
        body.Should().Be(markdown);
    }

    [Fact]
    public void GetFrontMatterYaml_WithValidFrontMatter_ReturnsYaml()
    {
        // Arrange
        var markdown = @"---
title: Test Title
slug: test-slug
---

# Content Here";

        // Act
        var yaml = markdown.GetFrontMatterYaml();

        // Assert
        yaml.Should().NotBeNull();
        yaml.Should().Contain("title: Test Title");
        yaml.Should().Contain("slug: test-slug");
    }

    [Fact]
    public void GetFrontMatterYaml_WithoutFrontMatter_ReturnsNull()
    {
        // Arrange
        var markdown = "# Just Markdown\n\nNo front matter here.";

        // Act
        var yaml = markdown.GetFrontMatterYaml();

        // Assert
        yaml.Should().BeNull();
    }
}
