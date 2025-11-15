using System;
using System.IO;

namespace Markdn.SourceGenerators.Generators;

/// <summary>
/// Shared helpers for resolving component namespaces and project roots based on markdown file paths.
/// </summary>
internal static class ComponentPathUtilities
{
    private static readonly string[] CommonDirectories = { "Pages", "Components", "Shared", "Views" };

    /// <summary>
    /// Attempts to determine the project root (from a markdown/component perspective) by walking up
    /// the directory path and locating common Blazor folders (Pages/Components/etc). Falls back to
    /// the file's directory when no known folder is found.
    /// </summary>
    public static string GetProjectRootFromPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? filePath;

        foreach (var dir in CommonDirectories)
        {
            var segment = Path.DirectorySeparatorChar + dir;
            var altSegment = Path.AltDirectorySeparatorChar + dir;

            var index = directory.LastIndexOf(segment, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                return directory.Substring(0, index);
            }

            index = directory.LastIndexOf(altSegment, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                return directory.Substring(0, index);
            }
        }

        return directory;
    }

    /// <summary>
    /// Computes the namespace used for a generated component given the consuming project's root namespace.
    /// </summary>
    public static string GetComponentNamespace(string rootNamespace, string filePath)
    {
        var projectRoot = GetProjectRootFromPath(filePath);
        return NamespaceGenerator.Generate(rootNamespace, filePath, projectRoot);
    }
}
