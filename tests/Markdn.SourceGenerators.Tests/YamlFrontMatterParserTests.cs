using System;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void Parse_NestedObject_IsStoredAsVariable()
        {
            var content = @"---
title: Test Page
variables:
    person:
        name: John
        age: 30
---
# Content";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.Equal("Test Page", metadata.Title);
            Assert.NotNull(metadata.Variables);
            Assert.True(metadata.Variables.ContainsKey("person"));
            
            var person = metadata.Variables["person"];
            Assert.IsType<Dictionary<string, object>>(person);
            var personDict = (Dictionary<string, object>)person;
            Assert.Equal("John", personDict["name"]);
            Assert.Equal(30, personDict["age"]);
        }

        [Fact]
        public void Parse_ArrayOfObjects_IsStoredAsVariable()
        {
            var content = @"---
title: Test Page
variables:
    products:
        - name: Product A
          price: 100
        - name: Product B
          price: 200
---
# Content";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.NotNull(metadata.Variables);
            Assert.True(metadata.Variables.ContainsKey("products"));
            
            var products = metadata.Variables["products"];
            Assert.IsType<List<object>>(products);
            var productsList = (List<object>)products;
            Assert.Equal(2, productsList.Count);
            
            var product1 = Assert.IsType<Dictionary<string, object>>(productsList[0]);
            Assert.Equal("Product A", product1["name"]);
            Assert.Equal(100, product1["price"]);
            
            var product2 = Assert.IsType<Dictionary<string, object>>(productsList[1]);
            Assert.Equal("Product B", product2["name"]);
            Assert.Equal(200, product2["price"]);
        }

        [Fact]
        public void Parse_MultiLineString_IsPreserved()
        {
            var content = @"---
variables:
    address: |
        123 Main St
        Suite 100
---
# Content";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.NotNull(metadata.Variables);
            Assert.True(metadata.Variables.ContainsKey("address"));
            
            var address = metadata.Variables["address"];
            Assert.IsType<string>(address);
            var addressStr = (string)address;
            Assert.Contains("123 Main St", addressStr);
            Assert.Contains("Suite 100", addressStr);
        }

        [Fact]
        public void Parse_YamlAnchorsAndAliases_AtTopLevel_AreResolved()
        {
            var content = @"---
bill-to: &id001
    name: John
    address: 123 Main
ship-to: *id001
---
# Content";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.NotNull(metadata.Variables);
            Assert.True(metadata.Variables.ContainsKey("bill-to"));
            Assert.True(metadata.Variables.ContainsKey("ship-to"));
            
            var billTo = Assert.IsType<Dictionary<string, object>>(metadata.Variables["bill-to"]);
            var shipTo = metadata.Variables["ship-to"];
            
            // ship-to should be the same object as bill-to (alias resolved)
            // For now, just check they're both dictionaries with same content
            if (shipTo is Dictionary<string, object> shipToDict)
            {
                Assert.Equal(billTo["name"], shipToDict["name"]);
                Assert.Equal(billTo["address"], shipToDict["address"]);
            }
            else
            {
                // If shipTo is the exact same reference, that's also valid
                Assert.Same(billTo, shipTo);
            }
        }

        [Fact]
        public void Parse_ParametersWithDefaultValues_AreParsed()
        {
            var content = @"---
parameters:
    pageSize: 10
    enabled: false
---
# Content";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.NotNull(metadata.Parameters);
            Assert.Equal(2, metadata.Parameters.Count);
            
            var pageSizeParam = metadata.Parameters.FirstOrDefault(p => p.Name == "PageSize");
            Assert.NotNull(pageSizeParam);
            Assert.Equal("int", pageSizeParam.Type);
            Assert.Equal("10", pageSizeParam.DefaultValue);
            
            var enabledParam = metadata.Parameters.FirstOrDefault(p => p.Name == "Enabled");
            Assert.NotNull(enabledParam);
            Assert.Equal("bool", enabledParam.Type);
            Assert.Equal("false", enabledParam.DefaultValue);
        }

        [Fact]
        public void Parse_ComplexNestedStructure_IsFullyParsed()
        {
            var content = @"---
variables:
    invoice: 34843
    bill-to:
        given: Chris
        family: Dumars
        address:
            lines: |
                458 Walkman Dr.
                Suite #292
            city: Royal Oak
            state: MI
---
# Content";

            var (metadata, markdown, errors) = YamlFrontMatterParser.Parse(content);

            Assert.NotNull(metadata);
            Assert.Empty(errors);
            Assert.NotNull(metadata.Variables);
            
            // Check invoice
            Assert.True(metadata.Variables.ContainsKey("invoice"));
            Assert.Equal(34843, metadata.Variables["invoice"]);
            
            // Check nested bill-to structure
            Assert.True(metadata.Variables.ContainsKey("bill-to"));
            var billTo = Assert.IsType<Dictionary<string, object>>(metadata.Variables["bill-to"]);
            Assert.Equal("Chris", billTo["given"]);
            Assert.Equal("Dumars", billTo["family"]);
            
            // Check nested address
            var address = Assert.IsType<Dictionary<string, object>>(billTo["address"]);
            Assert.Contains("458 Walkman Dr", (string)address["lines"]);
            Assert.Equal("Royal Oak", address["city"]);
            Assert.Equal("MI", address["state"]);
        }
    }
}
