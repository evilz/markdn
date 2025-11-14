using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Markdn.SourceGenerators;

/// <summary>
/// Incremental source generator that creates a typed content service for content collections.
/// Inspired by Astro's Content Collections API (getCollection, getEntry).
/// </summary>
[Generator]
public class ContentCollectionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter for collections.json file
        var collectionsConfig = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith("collections.json", StringComparison.OrdinalIgnoreCase));

        // Combine with compilation to get assembly information
        var configWithCompilation = collectionsConfig.Combine(context.CompilationProvider);

        // Read build property RootNamespace
        var rootNamespaceProvider = context.AnalyzerConfigOptionsProvider.Select((opts, ct) =>
        {
            if (opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var val))
            {
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
            return null;
        });

        // Combine all inputs
        var combined = configWithCompilation.Combine(rootNamespaceProvider);

        // Generate source for collections.json
        context.RegisterSourceOutput(combined, (spc, input) =>
        {
            try
            {
                var ((file, compilation), providedRootNamespace) = input;
                GenerateContentService(spc, file, compilation, providedRootNamespace);
            }
            catch (Exception ex)
            {
                var diag = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "MDCC001",
                        "Content Collections Generator Error",
                        $"Error during content collections generation: {ex.Message}",
                        "ContentCollections",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None);
                spc.ReportDiagnostic(diag);
            }
        });
    }

    private static void GenerateContentService(
        SourceProductionContext context,
        AdditionalText file,
        Compilation compilation,
        string? providedRootNamespace)
    {
        var sourceText = file.GetText(context.CancellationToken);
        if (sourceText == null)
        {
            return;
        }

        try
        {
            var content = sourceText.ToString();
            
            // Parse collections.json
            var collections = ParseCollectionsJson(content);
            if (collections == null || collections.Count == 0)
            {
                return;
            }

            // Determine namespace
            var rootNamespace = !string.IsNullOrWhiteSpace(providedRootNamespace)
                ? providedRootNamespace!
                : (compilation.AssemblyName ?? "Generated");

            // Generate the content service
            var serviceCode = GenerateServiceCode(rootNamespace, collections);
            
            context.AddSource(
                "ContentCollections.g.cs",
                SourceText.From(serviceCode, Encoding.UTF8));

            // Generate collection model classes
            foreach (var collection in collections)
            {
                var modelCode = GenerateCollectionModelCode(rootNamespace, collection);
                context.AddSource(
                    $"Collections.{collection.Name}.g.cs",
                    SourceText.From(modelCode, Encoding.UTF8));
            }
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "MDCC002",
                    "Content Collections Parsing Error",
                    $"Error parsing collections.json: {ex.Message}",
                    "ContentCollections",
                    DiagnosticSeverity.Error,
                    true),
                Location.None);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static List<CollectionInfo>? ParseCollectionsJson(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("collections", out var collectionsElement))
            {
                return null;
            }

            var collections = new List<CollectionInfo>();

            foreach (var collectionProperty in collectionsElement.EnumerateObject())
            {
                var collectionName = collectionProperty.Name;
                var collectionObj = collectionProperty.Value;

                var folderPath = collectionObj.TryGetProperty("folder", out var folder) 
                    ? folder.GetString() ?? collectionName 
                    : collectionName;

                // Parse schema properties to extract field definitions
                var fields = new List<FieldInfo>();
                if (collectionObj.TryGetProperty("schema", out var schema) &&
                    schema.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                    {
                        var fieldName = prop.Name;
                        var fieldType = "string"; // default
                        var isRequired = false;

                        string? jsonType = null;
                        string? format = null;

                        if (prop.Value.TryGetProperty("type", out var typeElement))
                        {
                            jsonType = typeElement.GetString();
                        }

                        if (prop.Value.TryGetProperty("format", out var formatElement))
                        {
                            format = formatElement.GetString();
                        }

                        fieldType = MapJsonTypeToCSharpForParsing(jsonType, format);

                        // Check if field is in required array
                        if (schema.TryGetProperty("required", out var requiredArray))
                        {
                            foreach (var req in requiredArray.EnumerateArray())
                            {
                                if (req.GetString() == fieldName)
                                {
                                    isRequired = true;
                                    break;
                                }
                            }
                        }

                        fields.Add(new FieldInfo
                        {
                            Name = fieldName,
                            Type = fieldType,
                            IsRequired = isRequired
                        });
                    }
                }

                collections.Add(new CollectionInfo
                {
                    Name = collectionName,
                    FolderPath = folderPath,
                    Fields = fields
                });
            }

            return collections;
        }
        catch
        {
            return null;
        }
    }

    private static string MapJsonTypeToCSharp(string? jsonType)
    {
        return jsonType switch
        {
            "string" => "string",
            "number" => "double",
            "integer" => "int",
            "boolean" => "bool",
            "array" => "List<string>",
            _ => "object"
        };
    }

    private static string MapJsonTypeToCSharpForParsing(string? jsonType, string? format)
    {
        if (jsonType == "string" && format == "date-time")
        {
            return "DateTime";
        }
        return MapJsonTypeToCSharp(jsonType);
    }

    private static string GenerateServiceCode(string rootNamespace, List<CollectionInfo> collections)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated by Markdn.SourceGenerators />");
        sb.AppendLine("// This file provides typed access to content collections.");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Content;");
        sb.AppendLine();
        
        // Generate interface for each collection
        foreach (var collection in collections)
        {
            var pascalName = ToPascalCase(collection.Name);
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Service for accessing the '{collection.Name}' collection.");
            sb.AppendLine("/// Inspired by Astro's Content Collections API.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public interface I{pascalName}Service");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>Gets all items from the '{collection.Name}' collection (getCollection).</summary>");
            sb.AppendLine($"    List<{pascalName}Entry> GetCollection();");
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>Gets a single item from the '{collection.Name}' collection by slug (getEntry).</summary>");
            sb.AppendLine($"    {pascalName}Entry? GetEntry(string slug);");
            sb.AppendLine("}");
            sb.AppendLine();
        }
        
        // Generate implementation for each collection
        foreach (var collection in collections)
        {
            var pascalName = ToPascalCase(collection.Name);
            
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Implementation of {pascalName} collection service.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public class {pascalName}Service : I{pascalName}Service");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly Lazy<List<{pascalName}Entry>> _entries;");
            sb.AppendLine();
            sb.AppendLine($"    public {pascalName}Service()");
            sb.AppendLine("    {");
            sb.AppendLine($"        _entries = new Lazy<List<{pascalName}Entry>>(() => LoadCollection());");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public List<{pascalName}Entry> GetCollection()");
            sb.AppendLine("    {");
            sb.AppendLine("        return _entries.Value;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public {pascalName}Entry? GetEntry(string slug)");
            sb.AppendLine("    {");
            sb.AppendLine("        return _entries.Value.FirstOrDefault(e => e.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    private List<{pascalName}Entry> LoadCollection()");
            sb.AppendLine("    {");
            sb.AppendLine("        var entries = new List<{pascalName}Entry>();");
            sb.AppendLine("        var assembly = Assembly.GetExecutingAssembly();");
            sb.AppendLine($"        var resourcePrefix = assembly.GetName().Name + \".{collection.FolderPath.Replace("/", ".").Replace("\\\\", ".")}\";");
            sb.AppendLine();
            sb.AppendLine("        var resourceNames = assembly.GetManifestResourceNames()");
            sb.AppendLine("            .Where(name => name.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(\".md\", StringComparison.OrdinalIgnoreCase))");
            sb.AppendLine("            .ToList();");
            sb.AppendLine();
            sb.AppendLine("        foreach (var resourceName in resourceNames)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                using var stream = assembly.GetManifestResourceStream(resourceName);");
            sb.AppendLine("                if (stream == null) continue;");
            sb.AppendLine();
            sb.AppendLine("                using var reader = new StreamReader(stream);");
            sb.AppendLine("                var content = reader.ReadToEnd();");
            sb.AppendLine();
            sb.AppendLine("                var entry = ParseMarkdownFile(content, resourceName);");
            sb.AppendLine("                if (entry != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    entries.Add(entry);");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            catch");
            sb.AppendLine("            {");
            sb.AppendLine("                // Skip files that can't be parsed");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        return entries;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    private {pascalName}Entry? ParseMarkdownFile(string content, string resourceName)");
            sb.AppendLine("    {");
            sb.AppendLine("        // Extract slug from resource name");
            sb.AppendLine("        var lastDot = resourceName.LastIndexOf('.');");
            sb.AppendLine("        var secondLastDot = resourceName.LastIndexOf('.', lastDot - 1);");
            sb.AppendLine("        var slug = secondLastDot >= 0 ? resourceName.Substring(secondLastDot + 1, lastDot - secondLastDot - 1) : \"unknown\";");
            sb.AppendLine();
            sb.AppendLine("        // Simple frontmatter parser");
            sb.AppendLine("        if (!content.StartsWith(\"---\")) return null;");
            sb.AppendLine();
            sb.AppendLine("        var secondDelimiter = content.IndexOf(\"---\", 3);");
            sb.AppendLine("        if (secondDelimiter < 0) return null;");
            sb.AppendLine();
            sb.AppendLine("        var frontMatter = content.Substring(3, secondDelimiter - 3);");
            sb.AppendLine("        var body = content.Substring(secondDelimiter + 3).Trim();");
            sb.AppendLine();
            sb.AppendLine("        // Parse frontmatter fields");
            sb.AppendLine("        var lines = frontMatter.Split(new[] { '\\n', '\\r' }, StringSplitOptions.RemoveEmptyEntries);");
            sb.AppendLine("        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);");
            sb.AppendLine();
            sb.AppendLine("        foreach (var line in lines)");
            sb.AppendLine("        {");
            sb.AppendLine("            var colonIndex = line.IndexOf(':');");
            sb.AppendLine("            if (colonIndex > 0)");
            sb.AppendLine("            {");
            sb.AppendLine("                var key = line.Substring(0, colonIndex).Trim();");
            sb.AppendLine("                var value = line.Substring(colonIndex + 1).Trim().Trim('\"').Trim('\\'');");
            sb.AppendLine("                fields[key] = value;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Create entry with parsed data");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new {pascalName}Entry");
            sb.AppendLine("            {");
            sb.AppendLine("                Slug = fields.ContainsKey(\"slug\") ? fields[\"slug\"] : slug,");
            sb.AppendLine("                Content = body,");
            
            // Add field assignments
            foreach (var field in collection.Fields)
            {
                var propName = ToPascalCase(field.Name);
                if (field.Type == "DateTime")
                {
                    sb.AppendLine($"                {propName} = fields.ContainsKey(\"{field.Name}\") && DateTime.TryParse(fields[\"{field.Name}\"], out var {field.Name}Val) ? {field.Name}Val : {(field.IsRequired ? "DateTime.UtcNow" : "default")},");
                }
                else if (field.Type == "string")
                {
                    sb.AppendLine($"                {propName} = fields.ContainsKey(\"{field.Name}\") ? fields[\"{field.Name}\"] : {(field.IsRequired ? "string.Empty" : "null")},");
                }
                else
                {
                    sb.AppendLine($"                {propName} = fields.ContainsKey(\"{field.Name}\") ? fields[\"{field.Name}\"] : default,");
                }
            }
            
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine("        catch");
            sb.AppendLine("        {");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private static string GenerateCollectionModelCode(string rootNamespace, CollectionInfo collection)
    {
        var sb = new StringBuilder();
        var pascalName = ToPascalCase(collection.Name);
        
        sb.AppendLine("// <auto-generated by Markdn.SourceGenerators />");
        sb.AppendLine($"// Model for '{collection.Name}' collection.");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Content;");
        sb.AppendLine();
        
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents an entry in the '{collection.Name}' collection.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {pascalName}Entry");
        sb.AppendLine("{");
        
        // Add standard properties
        sb.AppendLine("    /// <summary>The unique slug/identifier for this entry.</summary>");
        sb.AppendLine("    public required string Slug { get; init; }");
        sb.AppendLine();
        
        sb.AppendLine("    /// <summary>The markdown content body.</summary>");
        sb.AppendLine("    public string Content { get; init; } = string.Empty;");
        sb.AppendLine();
        
        // Add schema-defined properties
        foreach (var field in collection.Fields)
        {
            var propertyName = ToPascalCase(field.Name);
            var propertyType = field.Type;
            
            // Make nullable if not required and is reference type
            if (!field.IsRequired && (propertyType == "string" || propertyType.Contains("List")))
            {
                propertyType += "?";
            }
            
            sb.AppendLine($"    /// <summary>{propertyName} from frontmatter.</summary>");
            
            if (field.IsRequired && (field.Type == "string" || field.Type.Contains("List")))
            {
                sb.AppendLine($"    public required {propertyType} {propertyName} {{ get; init; }}");
            }
            else
            {
                var defaultValue = GetDefaultValue(field.Type, field.IsRequired);
                sb.AppendLine($"    public {propertyType} {propertyName} {{ get; init; }}{defaultValue}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static string GetDefaultValue(string type, bool isRequired)
    {
        if (isRequired)
        {
            return string.Empty;
        }
        
        return type switch
        {
            "string" => " = string.Empty;",
            "int" => " = 0;",
            "double" => " = 0.0;",
            "bool" => " = false;",
            _ when type.Contains("List") => " = new();",
            _ => " = default!;"
        };
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Split by common delimiters
        var parts = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.Substring(1));
                }
            }
        }

        return sb.ToString();
    }

    private class CollectionInfo
    {
        public required string Name { get; set; }
        public required string FolderPath { get; set; }
        public required List<FieldInfo> Fields { get; set; }
    }

    private class FieldInfo
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required bool IsRequired { get; set; }
    }
}
