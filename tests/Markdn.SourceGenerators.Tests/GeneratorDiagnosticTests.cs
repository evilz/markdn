using Xunit;
using Microsoft.CodeAnalysis;
using Markdn.SourceGenerators.Diagnostics;

namespace Markdn.SourceGenerators.Tests
{
    public class GeneratorDiagnosticTests
    {
        [Fact]
        public void MD001_InvalidYamlFrontMatter_DescriptorExistsAndFormats()
        {
            var desc = DiagnosticDescriptors.InvalidYamlFrontMatter;

            Assert.Equal("MD001", desc.Id);
            Assert.Equal(DiagnosticSeverity.Error, desc.DefaultSeverity);
            Assert.Contains("YAML", desc.Title.ToString());

            var diagnostic = Diagnostic.Create(desc, Location.None, "file.md", "some parse error");
            var message = diagnostic.GetMessage();

            Assert.Contains("file.md", message);
            Assert.Contains("some parse error", message);
        }
    }
}
