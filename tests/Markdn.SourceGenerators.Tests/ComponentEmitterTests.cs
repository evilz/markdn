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
            var componentTypeMap = new Dictionary<string, string> { { "Counter", "MyApp.Components" } };

            var html = "<Counter />";

            var generated = ComponentCodeEmitter.Emit("WithComponents", "MyApp.Pages", html, metadata, null, componentTypeMap, available);

            Assert.Contains("using MyApp.Components;", generated);
            Assert.Contains("builder.OpenComponent<Counter>", generated);
        }

        [Fact]
        public void Emit_Uses_ComponentName_With_UsingDirective_When_ComponentTypeMap_Provided()
        {
            var metadata = new ComponentMetadata();
            var componentTypeMap = new Dictionary<string, string> { { "Counter", "MyApp.Components" } };
            var available = new List<string> { "MyApp.Components" };
            var generated = ComponentCodeEmitter.Emit("WithComponents", "MyApp.Pages", "<Counter />", metadata, null, componentTypeMap, available);

            // Should emit using directive for the component namespace
            Assert.Contains("using MyApp.Components;", generated);
            // Should use simple component name
            Assert.Contains("builder.OpenComponent<Counter>", generated);
        }

        [Fact]
        public void Emit_Variables_AsDynamic_WithExpandoObject()
        {
            var variables = new Dictionary<string, object>
            {
                { "invoice", 34843 },
                { "date", "2001-01-23" },
                { "person", new Dictionary<string, object> 
                    { 
                        { "name", "John" }, 
                        { "age", 30 } 
                    } 
                }
            };

            var metadata = new ComponentMetadata
            {
                Variables = variables
            };

            var generated = ComponentCodeEmitter.Emit("TestComponent", "MyApp.Pages", "<p>Test</p>", metadata);

            // Check that variables are generated as dynamic fields
            Assert.Contains("private dynamic invoice = 34843;", generated);
            Assert.Contains("private dynamic date = @\"2001-01-23\";", generated);
            
            // Check that nested objects use ExpandoObject
            Assert.Contains("private dynamic person = (dynamic)new System.Dynamic.ExpandoObject()", generated);
            Assert.Contains("Name = @\"John\"", generated);
            Assert.Contains("Age = 30", generated);
        }

        [Fact]
        public void Emit_Variables_WithSanitizedNames()
        {
            var variables = new Dictionary<string, object>
            {
                { "bill-to", new Dictionary<string, object> { { "name", "John" } } },
                { "ship_to", "123 Main St" }
            };

            var metadata = new ComponentMetadata
            {
                Variables = variables
            };

            var generated = ComponentCodeEmitter.Emit("TestComponent", "MyApp.Pages", "<p>Test</p>", metadata);

            // Check that field names are sanitized (camelCase, no hyphens/underscores)
            Assert.Contains("private dynamic billTo", generated);
            Assert.Contains("private dynamic shipTo", generated);
        }

        [Fact]
        public void Emit_Parameters_WithDefaultValues()
        {
            var parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition { Name = "PageSize", Type = "int", DefaultValue = "10" },
                new ParameterDefinition { Name = "Enabled", Type = "bool", DefaultValue = "false" }
            };

            var metadata = new ComponentMetadata
            {
                Parameters = parameters
            };

            var generated = ComponentCodeEmitter.Emit("TestComponent", "MyApp.Pages", "<p>Test</p>", metadata);

            // Check that parameters have default values
            Assert.Contains("public int PageSize { get; set; } = 10;", generated);
            Assert.Contains("public bool Enabled { get; set; } = false;", generated);
        }
    }
}
