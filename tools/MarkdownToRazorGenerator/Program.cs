using System.Text;
using MarkdownToRazorGenerator.Generators;
using MarkdownToRazorGenerator.Parsers;
using MarkdownToRazorGenerator.Utilities;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace MarkdownToRazorGenerator;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Markdown to Razor Generator ===");
            Console.WriteLine();

            // Parse arguments
            var config = ParseArguments(args);
            if (config == null)
            {
                PrintUsage();
                return 1;
            }

            Console.WriteLine($"Project directory: {config.ProjectDirectory}");
            Console.WriteLine($"Markdown pattern: {config.MarkdownPattern}");
            Console.WriteLine();

            // Initialize components
            var frontMatterParser = new FrontMatterParser();
            var markdownConverter = new MarkdownConverter();
            var razorGenerator = new RazorComponentGenerator();

            int totalFiles = 0;
            int generatedFiles = 0;
            var errors = new List<string>();

            // Use globbing to find markdown files
            var matcher = new Matcher();
            matcher.AddInclude(config.MarkdownPattern);
            
            var matchResult = matcher.Execute(
                new DirectoryInfoWrapper(new DirectoryInfo(config.ProjectDirectory)));
            var markdownFiles = matchResult.Files.Select(f => Path.Combine(config.ProjectDirectory, f.Path)).ToList();

            totalFiles = markdownFiles.Count;
            Console.WriteLine($"Found {totalFiles} markdown files matching pattern");
            Console.WriteLine();

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(config.ProjectDirectory, filePath);
                    Console.WriteLine($"  Processing: {relativePath}");

                    // Read file content
                    var content = File.ReadAllText(filePath);

                    // Parse front-matter
                    var (metadata, markdownBody, parseErrors) = frontMatterParser.Parse(content);

                    if (parseErrors.Count > 0)
                    {
                        foreach (var error in parseErrors)
                        {
                            var errorMsg = $"{relativePath}: {error}";
                            errors.Add(errorMsg);
                            Console.WriteLine($"    Warning: {error}");
                        }
                    }

                    // Determine title (fallback chain)
                    var title = metadata.Title;
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = frontMatterParser.ExtractFirstH1(markdownBody);
                    }
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = Path.GetFileNameWithoutExtension(filePath);
                    }

                    // Determine slug (fallback chain)
                    var slug = metadata.Slug;
                    if (string.IsNullOrWhiteSpace(slug))
                    {
                        slug = SlugGenerator.Normalize(Path.GetFileNameWithoutExtension(filePath));
                    }

                    // Determine route (fallback chain)
                    var route = metadata.Route;
                    if (string.IsNullOrWhiteSpace(route))
                    {
                        // Extract directory context from file path for route generation
                        var fileDirectory = Path.GetDirectoryName(relativePath) ?? "";
                        var directoryType = ExtractDirectoryType(fileDirectory);
                        route = SlugGenerator.GenerateRoute(slug, directoryType);
                    }

                    // Convert markdown to HTML with section support
                    var (htmlContent, sections) = markdownConverter.ToHtmlWithSections(markdownBody);

                    // Generate Razor component with HTML content and sections
                    var razorContent = razorGenerator.Generate(metadata, htmlContent, route, title, sections);

                    // Write output file next to the original .md file
                    // Use PascalCase naming for Razor component compliance
                    var directory = Path.GetDirectoryName(filePath);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    
                    // Convert to PascalCase: capitalize first letter and after hyphens/underscores
                    var pascalCaseName = ToPascalCase(fileNameWithoutExt);
                    var outputFileName = $"{pascalCaseName}.razor";
                    var outputFilePath = Path.Combine(directory!, outputFileName);
                    File.WriteAllText(outputFilePath, razorContent);

                    Console.WriteLine($"    Generated: {outputFileName} (route: {route})");
                    generatedFiles++;
                }
                catch (Exception ex)
                {
                    var relativePath = Path.GetRelativePath(config.ProjectDirectory, filePath);
                    var errorMsg = $"{relativePath}: {ex.Message}";
                    errors.Add(errorMsg);
                    Console.WriteLine($"    Error: {ex.Message}");
                }
            }

            // Report results
            Console.WriteLine();
            Console.WriteLine($"Total markdown files found: {totalFiles}");
            Console.WriteLine($"Razor files generated: {generatedFiles}");

            if (errors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Errors encountered: {errors.Count}");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Generation complete!");
            return errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    /// <summary>
    /// Extracts directory type (blog, pages, etc.) from file path
    /// </summary>
    private static string ExtractDirectoryType(string fileDirectory)
    {
        var parts = fileDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Look for common directory names
        foreach (var part in parts)
        {
            var lowerPart = part.ToLowerInvariant();
            if (lowerPart == "blog" || lowerPart == "pages" || lowerPart == "docs" || lowerPart == "articles")
            {
                return lowerPart;
            }
        }
        
        // Default to empty which will generate simple routes
        return "";
    }

    private static GeneratorConfig? ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var config = new GeneratorConfig
        {
            ProjectDirectory = args[0],
            MarkdownPattern = "content/**/*.md"  // Default pattern
        };

        // Parse optional arguments
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--pattern" && i + 1 < args.Length)
            {
                config.MarkdownPattern = args[i + 1];
                i++;
            }
        }

        return config;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: MarkdownToRazorGenerator <project-directory> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --pattern <glob>      Glob pattern for markdown files (default: content/**/*.md)");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  MarkdownToRazorGenerator C:\\MyProject --pattern \"content/**/*.md\"");
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Split by common separators (hyphen, underscore, space)
        var parts = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                // Capitalize first letter of each part
                result.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    result.Append(part.Substring(1));
                }
            }
        }

        return result.ToString();
    }

    private class GeneratorConfig
    {
        public required string ProjectDirectory { get; set; }
        public required string MarkdownPattern { get; set; }
    }
}
