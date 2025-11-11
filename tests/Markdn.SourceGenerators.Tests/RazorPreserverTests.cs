using System;
using Xunit;
using Markdn.SourceGenerators.Parsers;

namespace Markdn.SourceGenerators.Tests
{
    public class RazorPreserverTests
    {
        [Fact]
        public void ExtractRazorSyntax_UnmatchedCodeBlock_RegistersError()
        {
            var content = "# Title\n\n@code {\n    private int x = 1;\n\n"; // missing closing brace
            var preserver = new RazorPreserver();

            var placeholder = preserver.ExtractRazorSyntax(content);
            var errors = preserver.GetErrors();

            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("@code"));
        }

        [Fact]
        public void ExtractRazorSyntax_UnmatchedControlFlow_RegistersError()
        {
            var content = "Paragraph\n\n@if (true) {\n    <p>Open block"; // missing closing brace
            var preserver = new RazorPreserver();

            var placeholder = preserver.ExtractRazorSyntax(content);
            var errors = preserver.GetErrors();

            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("control flow") || e.Contains("@if"));
        }
    }
}
