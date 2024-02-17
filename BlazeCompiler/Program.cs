using Blaze.Diagnostics;
using Blaze.IO;
using Mono.Options;
using System.Collections.Immutable;

namespace Blaze
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string? outputPath = null;
            string? moduleName = null;
            bool helpRequested = false;
            List<string> referencePaths = new List<string>();
            List<string> sourcePaths = new List<string>();

            OptionSet options = new OptionSet
            {
                "usage: BlazeCompiler <source-paths> [options]",
                { "r=", "The {path} of an assembly to reference", v => referencePaths.Add(v) },
                { "o=", "The output {path} of the assembly to create", v => outputPath = v },
                { "m=", "The {name} of the module", v => moduleName = v },
                { "?|h|help", v => helpRequested = true },
                { "<>", v => sourcePaths.Add(v) },
            };
            options.Parse(args);

            if (helpRequested)
            {
                options.WriteOptionDescriptions(Console.Out);
                Console.ReadKey();
                return 0;
            }

            if (sourcePaths.Count == 0)
            {
                Console.Error.WriteLine("error: need at least one source file");
                return 1;
            }

            if (outputPath == null)
                outputPath = Path.ChangeExtension(sourcePaths[0], ".exe");       

            if (moduleName == null)
                moduleName = Path.GetFileNameWithoutExtension(outputPath);

            bool hasErrors = false;
            List<SyntaxTree> trees = new List<SyntaxTree>();
            foreach (string path in sourcePaths)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"error: file {path} does not exist");
                    hasErrors = true;
                    continue;
                }
                SyntaxTree syntaxTree = SyntaxTree.Load(path);
                trees.Add(syntaxTree);
            }

            foreach (string path in referencePaths)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"error: file {path} does not exist");
                    hasErrors = true;
                    continue;
                }
            }

            if (hasErrors)
                return 1;

            Compilation compilation = Compilation.Create(trees.ToArray());
            ImmutableArray<Diagnostic> diagnostics = compilation.Emit(moduleName, referencePaths.ToArray(), outputPath);

            if (diagnostics.Any())
            {
                Console.Error.WriteDiagnostics(diagnostics);
                return 1;
            }

            Console.WriteLine(outputPath);
            Console.ReadKey();
            return 0;
        }
    }
}