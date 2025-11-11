using System;
using Xunit;
using Markdn.SourceGenerators.Parsers;

namespace Markdn.SourceGenerators.Tests
{
    public class YamlFrontMatterParserTests
    {
        [Fact]
        public void Parse_ComponentNamespaces_Array_IsParsed()
        {
            var content = "---\ncomponentNamespaces:\n  - MyApp.Components\n  - MyApp.Shared\n---\n\n# Hello";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.NotNull(metadata.ComponentNamespaces);
            Assert.Equal(2, metadata.ComponentNamespaces.Count);
            Assert.Contains("MyApp.Components", metadata.ComponentNamespaces);
            Assert.Contains("MyApp.Shared", metadata.ComponentNamespaces);
            Assert.Contains("# Hello", markdown);
        }

        [Fact]
        public void Parse_MissingClosingDelimiter_ReturnsError()
        {
            var content = "---\ntitle: Test\n# missing closing delimiter\n# rest of file";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("missing closing '---'", StringComparison.OrdinalIgnoreCase));
            // When front matter is malformed, metadata should be empty and markdown should be original content
            Assert.Equal(string.Empty, metadata.Title ?? string.Empty);
            Assert.Equal(content, markdown);
        }

        [Fact]
        public void Parse_InvalidLineWithoutColon_ReturnsError()
        {
            var content = "---\ninvalidline\n---\n# body";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("missing ':'", StringComparison.OrdinalIgnoreCase));
            // Metadata should be empty because parsing failed for that line
            Assert.Equal(string.Empty, metadata.Title ?? string.Empty);
            // Markdown body should still be parsed (after closing delimiter)
            Assert.Contains("# body", markdown);
        }
    }
}
