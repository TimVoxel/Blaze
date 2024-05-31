using Blaze.IO;
using Mono.Options;
using System.Text.Json;

namespace Blaze
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var helpRequested = false;
            var projectPath = "";
            var options = new OptionSet
            {
                "usage: BlazeCompiler <project-json-path>",
                { "?|h|help", v => helpRequested = true },
                { "<>", v => projectPath = v },
            };

            options.Parse(args);

            if (helpRequested)
            {
                options.WriteOptionDescriptions(Console.Out);
                Console.ReadKey();
                return 0;
            }

            if (string.IsNullOrEmpty(projectPath))
            {
                Console.Error.WriteLine("error: project file not provided");
                Console.ReadKey();
                return 1;
            }

            if (!File.Exists(projectPath))
            {
                Console.WriteLine($"error: file {projectPath} does not exist");
                Console.ReadKey();
                return 1;
            }

            try
            {
                var configuration = CompilationConfiguration.FromJson(projectPath);
                if (configuration == null)
                {
                    Console.WriteLine($"error: couldn't parse {projectPath}");
                    Console.ReadKey();
                    return 1;
                }
                if (!configuration.OutputFolders.Any()) 
                {
                    Console.WriteLine($"error: specify at least one output folder");
                    Console.ReadKey();
                    return 1;
                }

                var projectDirectory = Path.GetDirectoryName(projectPath);

                if (projectDirectory == null)
                {
                    Console.WriteLine("error: project is not in a directory");
                    Console.ReadKey();
                    return 1;
                }

                var trees = new List<SyntaxTree>();
                var directoryScripts = GetAllScripts(projectDirectory);

                foreach (string path in directoryScripts)
                {
                    var syntaxTree = SyntaxTree.Load(path);
                    trees.Add(syntaxTree);
                }

                var compilation = Compilation.Create(configuration, trees.ToArray());
                var diagnostics = compilation.Emit();

                if (diagnostics.Any())
                {
                    Console.Error.WriteDiagnostics(diagnostics);
                    Console.ReadKey();
                    return 1;
                }

                foreach (string path in configuration.OutputFolders)
                    Console.WriteLine($"Successfully emmitted {configuration.Name} to {path}");

                Console.ReadKey();
                return 0;
            }
            catch (JsonException exception)
            {
                Console.WriteLine($"error: There appears to be a problem in the project file:");
                Console.WriteLine(exception.Message);
                Console.ReadKey();
                return 1;
            }
        }

        private static IEnumerable<string> GetAllScripts(string directory)
        {
            return Directory.GetFiles(directory).Where(f => f.EndsWith(".blz"));
        }
    }
}