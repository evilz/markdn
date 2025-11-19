using System;
using System.Collections.Generic;
using System.Text;
using Markdn.SourceGenerators.Models;

namespace Markdn.SourceGenerators.Emitters;

/// <summary>
/// Emits C# source code for Blazor components from ComponentMetadata.
/// Handles class structure, namespace, attributes, inheritance, and parameters.
/// </summary>
public static class ComponentCodeEmitter
{
    /// <summary>
    /// Determine if a type is a reference type (needs = default! initialization)
    /// </summary>
    private static bool IsReferenceType(string typeName)
    {
        // Common value types that don't need = default!
        var valueTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint",
            "long", "ulong", "short", "ushort", "nint", "nuint",
            "System.Boolean", "System.Byte", "System.SByte", "System.Char", "System.Decimal",
            "System.Double", "System.Single", "System.Int32", "System.UInt32", "System.Int64",
            "System.UInt64", "System.Int16", "System.UInt16", "System.IntPtr", "System.UIntPtr"
        };

        // If it's a known value type, it's not a reference type
        if (valueTypes.Contains(typeName))
        {
            return false;
        }

        // If it ends with ?, it's a nullable value type, still not a reference type
        if (typeName.EndsWith("?"))
        {
            return false;
        }

        // Everything else is assumed to be a reference type
        // This includes string, custom classes, interfaces, etc.
        return true;
    }

    /// <summary>
    /// Sanitize field name (convert kebab-case to camelCase, handle invalid chars)
    /// </summary>
    private static string SanitizeFieldName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "field";
        }

        var sb = new StringBuilder();
        bool capitalizeNext = false;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            if (c == '-' || c == '_' || c == ' ')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                if (sb.Length == 0)
                {
                    // First character should be lowercase
                    sb.Append(char.ToLower(c));
                    capitalizeNext = false;
                }
                else if (capitalizeNext)
                {
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "field" : result;
    }

    /// <summary>
    /// Generate ExpandoObject initializer from a value
    /// </summary>
    private static string GenerateExpandoInitializer(object? value, int indentLevel)
    {
        if (value == null)
        {
            return "null";
        }

        // Handle dictionaries (objects)
        if (value is Dictionary<string, object> dict)
        {
            return GenerateExpandoFromDictionary(dict, indentLevel);
        }

        // Handle lists (arrays)
        if (value is List<object> list)
        {
            return GenerateListInitializer(list, indentLevel);
        }

        // Handle scalar values
        return FormatScalarValue(value);
    }

    /// <summary>
    /// Generate ExpandoObject initializer from dictionary
    /// </summary>
    private static string GenerateExpandoFromDictionary(Dictionary<string, object> dict, int indentLevel)
    {
        if (dict.Count == 0)
        {
            return "new System.Dynamic.ExpandoObject()";
        }

        var sb = new StringBuilder();
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        sb.Append("(dynamic)new System.Dynamic.ExpandoObject()");
        sb.AppendLine(" {");

        bool first = true;
        foreach (var kvp in dict)
        {
            if (!first)
            {
                sb.AppendLine(",");
            }
            first = false;

            var propName = SanitizePropertyName(kvp.Key);
            var propValue = GenerateExpandoInitializer(kvp.Value, indentLevel + 1);
            sb.Append($"{innerIndent}{propName} = {propValue}");
        }

        sb.AppendLine();
        sb.Append($"{indent}}}");
        
        return sb.ToString();
    }

    /// <summary>
    /// Generate list initializer
    /// </summary>
    private static string GenerateListInitializer(List<object> list, int indentLevel)
    {
        if (list.Count == 0)
        {
            return "new System.Collections.Generic.List<dynamic>()";
        }

        var sb = new StringBuilder();
        var indent = new string(' ', indentLevel * 4);
        var innerIndent = new string(' ', (indentLevel + 1) * 4);

        sb.Append("new System.Collections.Generic.List<dynamic>");
        sb.AppendLine(" {");

        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine(",");
            }

            var itemValue = GenerateExpandoInitializer(list[i], indentLevel + 1);
            sb.Append($"{innerIndent}{itemValue}");
        }

        sb.AppendLine();
        sb.Append($"{indent}}}");
        
        return sb.ToString();
    }

    /// <summary>
    /// Sanitize property name for ExpandoObject (must be valid C# identifier)
    /// </summary>
    private static string SanitizePropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Property";
        }

        var sb = new StringBuilder();
        bool capitalizeNext = true;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            if (c == '-' || c == '_' || c == ' ')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                if (sb.Length == 0)
                {
                    // First character should be uppercase for property
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else if (capitalizeNext)
                {
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "Property" : result;
    }

    /// <summary>
    /// Format scalar value for code generation
    /// </summary>
    private static string FormatScalarValue(object value)
    {
        return value switch
        {
            string s => $"@\"{s.Replace("\"", "\"\"")}\"",
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            long l => l.ToString() + "L",
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f",
            _ => $"\"{value}\""
        };
    }
    /// <summary>
    /// Generate complete component source code.
    /// </summary>
    /// <param name="componentName">Component class name</param>
    /// <param name="namespaceValue">Namespace for the component</param>
    /// <param name="htmlContent">Rendered HTML content</param>
    /// <param name="metadata">Component metadata from YAML front matter</param>
    /// <param name="codeBlocks">Extracted @code blocks</param>
    /// <returns>Complete C# source code</returns>
    public static string Emit(
        string componentName,
        string namespaceValue,
        string htmlContent,
        ComponentMetadata metadata,
        List<CodeBlock>? codeBlocks = null,
        Dictionary<string, string>? componentTypeMap = null,
        IEnumerable<string>? availableNamespaces = null,
        string? routeSlug = null)
    {
        var sb = new StringBuilder();
        
        // File header
        sb.AppendLine("// <auto-generated by Markdn.SourceGenerators v1.0.0 />");
        sb.AppendLine("// This file is auto-generated. Do not edit directly.");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

    // NOTE: we may emit lightweight using directives for namespaces that are
    // known to exist in the current compilation (availableNamespaces). This
    // keeps generated files readable and avoids hard-coding a single
    // project-specific namespace while still enabling simple type lookup when
    // available.
    sb.AppendLine();

        // Using directives (from generator-discovered available namespaces)
        if (availableNamespaces != null)
        {
            foreach (var ns in availableNamespaces)
            {
                if (!string.IsNullOrWhiteSpace(ns))
                {
                    sb.AppendLine($"using {ns};");
                }
            }
            sb.AppendLine();
        }

        // Ensure the parent namespace of the generated component is available as a using
        // This helps resolve sibling components (e.g., Counter in Pages) without
        // requiring the generator to perfectly discover every type's namespace.
        if (!string.IsNullOrWhiteSpace(namespaceValue))
        {
            var lastDot = namespaceValue.LastIndexOf('.');
            if (lastDot > 0)
            {
                var parentNs = namespaceValue.Substring(0, lastDot);
                if (!string.IsNullOrWhiteSpace(parentNs))
                {
                    sb.AppendLine($"using {parentNs};");
                    sb.AppendLine();
                }
            }
        }

        // Using directives (from metadata)
        if (metadata.Using != null && metadata.Using.Count > 0)
        {
            foreach (var usingDirective in metadata.Using)
            {
                sb.AppendLine($"using {usingDirective};");
            }
            sb.AppendLine();
        }

        // Namespace
        sb.AppendLine($"namespace {namespaceValue}");
        sb.AppendLine("{");

        // Route attribute based on slug
        if (!string.IsNullOrWhiteSpace(routeSlug))
        {
            sb.AppendLine($"    [Microsoft.AspNetCore.Components.RouteAttribute(\"{routeSlug}\")]");
        }

        // T091: Layout attribute (from metadata)
        if (!string.IsNullOrEmpty(metadata.Layout))
        {
            sb.AppendLine($"    [Microsoft.AspNetCore.Components.LayoutAttribute(typeof({metadata.Layout}))]");
        }

        // Class attributes (from metadata)
        if (metadata.Attribute != null && metadata.Attribute.Count > 0)
        {
            foreach (var attribute in metadata.Attribute)
            {
                sb.AppendLine($"    [{attribute}]");
            }
        }

        // Class declaration with inheritance
        var baseClass = metadata.Inherit ?? "Microsoft.AspNetCore.Components.ComponentBase";
        sb.AppendLine($"    public partial class {componentName} : {baseClass}");
        sb.AppendLine("    {");

        // Component parameters
        if (metadata.Parameters != null && metadata.Parameters.Count > 0)
        {
            foreach (var parameter in metadata.Parameters)
            {
                sb.AppendLine("        [Microsoft.AspNetCore.Components.Parameter]");
                string defaultValue;
                if (!string.IsNullOrEmpty(parameter.DefaultValue))
                {
                    defaultValue = $" = {parameter.DefaultValue};";
                }
                else
                {
                    defaultValue = IsReferenceType(parameter.Type) ? " = default!;" : "";
                }
                sb.AppendLine($"        public {parameter.Type} {parameter.Name} {{ get; set; }}{defaultValue}");
                sb.AppendLine();
            }
        }

        // Variables as dynamic fields (using ExpandoObject)
        if (metadata.Variables != null && metadata.Variables.Count > 0)
        {
            sb.AppendLine("        // Variables from YAML front matter");
            foreach (var variable in metadata.Variables)
            {
                var fieldName = SanitizeFieldName(variable.Key);
                var expandoInitializer = GenerateExpandoInitializer(variable.Value, 2);
                sb.AppendLine($"        private dynamic {fieldName} = {expandoInitializer};");
            }
            sb.AppendLine();
        }

    // BuildRenderTree method (using RenderTreeBuilderEmitter)
    sb.Append(RenderTreeBuilderEmitter.EmitBuildRenderTree(htmlContent, metadata, componentTypeMap, indentLevel: 2));

        // T059: Emit @code blocks after BuildRenderTree method
        if (codeBlocks != null && codeBlocks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        // Code blocks from Markdown");
            
            foreach (var codeBlock in codeBlocks)
            {
                // Indent each line of the code block
                var lines = codeBlock.Content.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                foreach (var line in lines)
                {
                    sb.AppendLine($"        {line}");
                }
            }
        }

        // Close class and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate simple component source (legacy method for backward compatibility).
    /// </summary>
    public static string EmitSimple(
        string componentName,
        string namespaceValue,
        string htmlContent)
    {
        return Emit(componentName, namespaceValue, htmlContent, ComponentMetadata.Empty, null, null);
    }
}
