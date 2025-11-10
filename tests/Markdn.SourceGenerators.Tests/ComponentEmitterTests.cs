using System.Collections.Generic;
using Markdn.SourceGenerators.Emitters;
using Markdn.SourceGenerators.Models;
using Xunit;

namespace Markdn.SourceGenerators.Tests
{
    public class ComponentEmitterTests
    {
        [Fact]
        public void Emit_Includes_Using_When_AvailableNamespaces_Provided()
        {
            var metadata = new ComponentMetadata
            {
            };

            var available = new List<string> { "MyApp.Components" };

            var html = "<Counter />";

            var generated = ComponentCodeEmitter.Emit("WithComponents", "MyApp.Pages", html, metadata, null, null, available);

            Assert.Contains("using MyApp.Components;", generated);
            Assert.Contains("typeof(Counter)", generated);
        }

        [Fact]
        public void Emit_Uses_FullyQualified_When_ComponentTypeMap_Provided_And_No_AvailableNamespaces()
        {
            var metadata = new ComponentMetadata();
            var componentTypeMap = new Dictionary<string, string> { { "Counter", "MyApp.Components" } };
            var generated = ComponentCodeEmitter.Emit("WithComponents", "MyApp.Pages", "<Counter />", metadata, null, componentTypeMap, null);

            Assert.Contains("typeof(global::MyApp.Components.Counter)", generated);
        }
    }
}
