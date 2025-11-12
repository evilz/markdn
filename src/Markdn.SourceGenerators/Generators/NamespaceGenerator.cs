using System;
using System.IO;
using System.Text;

namespace Markdn.SourceGenerators.Generators;

/// <summary>
/// Generates namespace from directory structure.
/// </summary>
internal static class NamespaceGenerator
{
    public static string Generate(string rootNamespace, string filePath, string projectRoot)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        
        // Get relative path from project root (manual implementation for netstandard2.0)
        var relativePath = GetRelativePath(projectRoot, directory);
        
        // If at root or in root directory, just use root namespace
        if (string.IsNullOrEmpty(relativePath) || relativePath == "." || relativePath == string.Empty)
        {
            return rootNamespace;
        }

        // Convert path separators to dots and clean up
        var namespaceSuffix = relativePath
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.')
            .Trim('.');

        // Remove empty segments
        var segments = namespaceSuffix.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return rootNamespace;
        }

        namespaceSuffix = string.Join(".", segments);
        return $"{rootNamespace}.{namespaceSuffix}";
    }

    private static string GetRelativePath(string relativeTo, string path)
    {
        var relativeToUri = new Uri(EnsureTrailingSlash(relativeTo));
        var pathUri = new Uri(EnsureTrailingSlash(path));

        if (relativeToUri.Scheme != pathUri.Scheme)
        {
            return path; // Can't make relative
        }

        var relativeUri = relativeToUri.MakeRelativeUri(pathUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string EnsureTrailingSlash(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            return path + Path.DirectorySeparatorChar;
        }
        return path;
    }
}
