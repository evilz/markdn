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
    }
}
