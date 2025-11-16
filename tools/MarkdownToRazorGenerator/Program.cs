using MarkdownToRazorGenerator.Generators;
using MarkdownToRazorGenerator.Parsers;
using MarkdownToRazorGenerator.Utilities;

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
            Console.WriteLine($"Blog directory: {config.BlogDir}");
            Console.WriteLine($"Pages directory: {config.PagesDir}");
            Console.WriteLine($"Output root: {config.OutputRoot}");
            Console.WriteLine();

            // Initialize components
            var frontMatterParser = new FrontMatterParser();
            var markdownConverter = new MarkdownConverter();
            var razorGenerator = new RazorComponentGenerator();

            int totalFiles = 0;
            int generatedFiles = 0;
            var errors = new List<string>();

            // Process blog files
            if (!string.IsNullOrWhiteSpace(config.BlogDir))
            {
                var blogPath = Path.Combine(config.ProjectDirectory, config.BlogDir);
                var blogOutputPath = Path.Combine(config.ProjectDirectory, config.OutputRoot, "Blog");
                ProcessDirectory(blogPath, blogOutputPath, "blog", frontMatterParser, markdownConverter, razorGenerator, ref totalFiles, ref generatedFiles, errors);
            }

            // Process pages files
            if (!string.IsNullOrWhiteSpace(config.PagesDir))
            {
                var pagesPath = Path.Combine(config.ProjectDirectory, config.PagesDir);
                var pagesOutputPath = Path.Combine(config.ProjectDirectory, config.OutputRoot, "Pages");
                ProcessDirectory(pagesPath, pagesOutputPath, "pages", frontMatterParser, markdownConverter, razorGenerator, ref totalFiles, ref generatedFiles, errors);
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

    private static void ProcessDirectory(
        string inputPath,
        string outputPath,
        string directoryType,
        FrontMatterParser frontMatterParser,
        MarkdownConverter markdownConverter,
        RazorComponentGenerator razorGenerator,
        ref int totalFiles,
        ref int generatedFiles,
        List<string> errors)
    {
        if (!Directory.Exists(inputPath))
        {
            Console.WriteLine($"Directory not found: {inputPath}");
            return;
        }

        Console.WriteLine($"Processing {directoryType} directory: {inputPath}");

        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);

        // Find all markdown files
        var markdownFiles = Directory.GetFiles(inputPath, "*.md", SearchOption.AllDirectories);
        totalFiles += markdownFiles.Length;

        foreach (var filePath in markdownFiles)
        {
            try
            {
                Console.WriteLine($"  Processing: {Path.GetFileName(filePath)}");

                // Read file content
                var content = File.ReadAllText(filePath);

                // Parse front-matter
                var (metadata, markdownBody, parseErrors) = frontMatterParser.Parse(content);

                if (parseErrors.Count > 0)
                {
                    foreach (var error in parseErrors)
                    {
                        var errorMsg = $"{Path.GetFileName(filePath)}: {error}";
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
                    route = SlugGenerator.GenerateRoute(slug, directoryType);
                }

                // Convert markdown to HTML
                var htmlContent = markdownConverter.ToHtml(markdownBody);

                // Generate Razor component
                var razorContent = razorGenerator.Generate(metadata, htmlContent, route, title);

                // Write output file
                // Capitalize first letter for Razor component naming convention
                var componentName = char.ToUpperInvariant(slug[0]) + slug.Substring(1);
                var outputFileName = $"{componentName}.razor";
                var outputFilePath = Path.Combine(outputPath, outputFileName);
                File.WriteAllText(outputFilePath, razorContent);

                Console.WriteLine($"    Generated: {outputFileName} (route: {route})");
                generatedFiles++;
            }
            catch (Exception ex)
            {
                var errorMsg = $"{Path.GetFileName(filePath)}: {ex.Message}";
                errors.Add(errorMsg);
                Console.WriteLine($"    Error: {ex.Message}");
            }
        }
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
            BlogDir = "content/blog",
            PagesDir = "content/pages",
            OutputRoot = "Generated"
        };

        // Parse optional arguments
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--blogDir" && i + 1 < args.Length)
            {
                config.BlogDir = args[i + 1];
                i++;
            }
            else if (args[i] == "--pagesDir" && i + 1 < args.Length)
            {
                config.PagesDir = args[i + 1];
                i++;
            }
            else if (args[i] == "--outputRoot" && i + 1 < args.Length)
            {
                config.OutputRoot = args[i + 1];
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
        Console.WriteLine("  --blogDir <path>      Path to blog markdown files (default: content/blog)");
        Console.WriteLine("  --pagesDir <path>     Path to pages markdown files (default: content/pages)");
        Console.WriteLine("  --outputRoot <path>   Root directory for generated files (default: Generated)");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  MarkdownToRazorGenerator C:\\MyProject --blogDir content/blog --outputRoot Generated");
    }

    private class GeneratorConfig
    {
        public required string ProjectDirectory { get; set; }
        public required string BlogDir { get; set; }
        public required string PagesDir { get; set; }
        public required string OutputRoot { get; set; }
    }
}
