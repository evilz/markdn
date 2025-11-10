using System;
using System.Text;
using Markdn.SourceGenerators.Models;

namespace Markdn.SourceGenerators.Generators;

/// <summary>
/// Generates component class name from filename.
/// </summary>
internal static class ComponentNameGenerator
{
    public static string Generate(string fileName)
    {
        var nameWithoutExtension = fileName;
        
        // Remove .md extension
        if (nameWithoutExtension.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            nameWithoutExtension = nameWithoutExtension.Substring(0, nameWithoutExtension.Length - 3);
        }

        // Remove date prefix (YYYY-MM-DD-)
        if (nameWithoutExtension.Length > 11 &&
            char.IsDigit(nameWithoutExtension[0]) &&
            nameWithoutExtension[4] == '-' &&
            nameWithoutExtension[7] == '-' &&
            nameWithoutExtension[10] == '-')
        {
            nameWithoutExtension = nameWithoutExtension.Substring(11);
        }

        // Convert kebab-case to PascalCase
        var parts = nameWithoutExtension.Split('-', '_');
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

        var className = sb.ToString();

        // Handle reserved keywords
        if (IsReservedKeyword(className))
        {
            className = "@" + className;
        }

        return className;
    }

    private static bool IsReservedKeyword(string name)
    {
        return name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or
            "catch" or "char" or "checked" or "class" or "const" or "continue" or
            "decimal" or "default" or "delegate" or "do" or "double" or "else" or
            "enum" or "event" or "explicit" or "extern" or "false" or "finally" or
            "fixed" or "float" or "for" or "foreach" or "goto" or "if" or "implicit" or
            "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or
            "namespace" or "new" or "null" or "object" or "operator" or "out" or
            "override" or "params" or "private" or "protected" or "public" or "readonly" or
            "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or
            "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or
            "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or
            "using" or "virtual" or "void" or "volatile" or "while" => true,
            _ => false
        };
    }
}
