using Xunit;
using Markdn.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Markdn.SourceGenerators.Tests
{
    public class DiagnosticDescriptorTests
    {
        [Fact]
        public void MD006_UnresolvableComponentReference_ExistsAndIsInfo()
        {
            var desc = DiagnosticDescriptors.UnresolvableComponentReference;

            Assert.Equal("MD006", desc.Id);
            Assert.Equal(DiagnosticSeverity.Info, desc.DefaultSeverity);
            Assert.Contains("Component", desc.Title.ToString());
            Assert.Contains("{0}", desc.MessageFormat.ToString());
        }
    }
}
