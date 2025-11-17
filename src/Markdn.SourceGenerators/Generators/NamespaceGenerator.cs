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

        var relativePath = GetRelativePath(projectRoot, directory);

        if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
        {
            return rootNamespace;
        }

        var namespaceSuffix = relativePath
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.')
            .Trim('.');

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
        // Normalize separators
        relativeTo = (relativeTo ?? string.Empty).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        path = (path ?? string.Empty).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (string.IsNullOrEmpty(path))
            return string.Empty;

        if (string.Equals(relativeTo, path, StringComparison.OrdinalIgnoreCase))
            return ".";

        // If both are rooted, use URI-based relative computation
        if (Path.IsPathRooted(relativeTo) && Path.IsPathRooted(path))
        {
            try
            {
                var baseUri = new Uri(AppendDirectorySeparator(relativeTo));
                var targetUri = new Uri(AppendDirectorySeparator(path));
                if (baseUri.Scheme == targetUri.Scheme)
                {
                    var relativeUri = baseUri.MakeRelativeUri(targetUri);
                    var rel = Uri.UnescapeDataString(relativeUri.ToString());
                    return rel.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
                }
            }
            catch
            {
                // fall back to manual
            }
        }

        // Manual fallback for relative/non-rooted inputs
        // Common case in tests: relativeTo == "pages", path == "pages" or a subfolder
        if (!string.IsNullOrEmpty(relativeTo) && path.StartsWith(relativeTo, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path.Substring(relativeTo.Length).TrimStart(Path.DirectorySeparatorChar);
            return string.IsNullOrEmpty(remainder) ? "." : remainder;
        }

        return path;
    }

    private static string AppendDirectorySeparator(string p)
    {
        if (string.IsNullOrEmpty(p)) return p;
        if (!p.EndsWith(Path.DirectorySeparatorChar.ToString()))
            return p + Path.DirectorySeparatorChar;
        return p;
    }
}
