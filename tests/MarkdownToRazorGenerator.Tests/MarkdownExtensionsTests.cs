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

    [Fact]
    public void GetFrontMatter_WithValidFrontMatter_ReturnsDictionary()
    {
        // Arrange
        var markdown = @"---
title: Test Title
slug: test-slug
customField: customValue
count: 42
---

# Content Here";

        // Act
        var frontMatter = markdown.GetFrontMatter();

        // Assert
        frontMatter.Should().NotBeNull();
        frontMatter.Should().ContainKey("title");
        frontMatter!["title"].Should().Be("Test Title");
        frontMatter.Should().ContainKey("slug");
        frontMatter["slug"].Should().Be("test-slug");
        frontMatter.Should().ContainKey("customField");
        frontMatter["customField"].Should().Be("customValue");
        frontMatter.Should().ContainKey("count");
        frontMatter["count"].Should().Be(42);
    }

    [Fact]
    public void GetFrontMatter_WithoutFrontMatter_ReturnsDictionaryNull()
    {
        // Arrange
        var markdown = "# Just Markdown\n\nNo front matter here.";

        // Act
        var frontMatter = markdown.GetFrontMatter();

        // Assert
        frontMatter.Should().BeNull();
    }

    [Fact]
    public void GetFrontMatter_WithComplexStructure_ReturnsDictionaryWithNestedObjects()
    {
        // Arrange
        var markdown = @"---
title: Test
tags:
  - tag1
  - tag2
metadata:
  author: John
  published: true
---

# Content";

        // Act
        var frontMatter = markdown.GetFrontMatter();

        // Assert
        frontMatter.Should().NotBeNull();
        frontMatter.Should().ContainKey("title");
        frontMatter.Should().ContainKey("tags");
        frontMatter.Should().ContainKey("metadata");
    }

    [Fact]
    public void GetFrontMatter_WithVariablesAndParameters_PreservesTypes()
    {
        // Arrange
        var markdown = @"---
title: Test Page
variables:
  count: 42
  price: 19.99
  isActive: true
  name: ""Test""
parameters:
  pageSize: 10
  enabled: false
  factor: 2.5
---

# Content";

        // Act
        var frontMatter = markdown.GetFrontMatter();

        // Assert
        frontMatter.Should().NotBeNull();
        frontMatter.Should().ContainKey("variables");
        frontMatter.Should().ContainKey("parameters");
        
        // Verify variables types are preserved
        var variables = frontMatter!["variables"] as Dictionary<object, object>;
        variables.Should().NotBeNull();
        // YamlDotNet may deserialize small integers as byte/short, so check for numeric types
        variables!["count"].Should().BeAssignableTo<IConvertible>().Which.ToInt32(null).Should().Be(42);
        // Check for floating point number (could be float or double)
        variables["price"].Should().BeAssignableTo<IConvertible>().Which.ToDouble(null).Should().BeApproximately(19.99, 0.001);
        variables["isActive"].Should().BeOfType<bool>().And.Be(true);
        variables["name"].Should().BeOfType<string>().And.Be("Test");
        
        // Verify parameters types are preserved
        var parameters = frontMatter["parameters"] as Dictionary<object, object>;
        parameters.Should().NotBeNull();
        parameters!["pageSize"].Should().BeAssignableTo<IConvertible>().Which.ToInt32(null).Should().Be(10);
        parameters["enabled"].Should().BeOfType<bool>().And.Be(false);
        parameters["factor"].Should().BeAssignableTo<IConvertible>().Which.ToDouble(null).Should().BeApproximately(2.5, 0.001);
    }
}
