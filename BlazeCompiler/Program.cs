using Blaze.IO;
using Blaze.Symbols;

namespace Blaze
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: BlazeCompiler <source-paths>");
                return;
            }
            
            IEnumerable<string> paths = GetFilePaths(args);
            bool hasErrors = false;
            List<SyntaxTree> trees = new List<SyntaxTree>();
            foreach (string path in paths)
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

            if (hasErrors) 
                return;

            Compilation compilation = Compilation.Create(trees.ToArray());
            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object?>());

            if (!result.Diagnostics.Any())
            {
                if (result.Value != null)
                    Console.Out.WriteLine(result.Value);
            }
            else
                Console.Error.WriteDiagnostics(result.Diagnostics);
            Console.ReadKey();
        }

        private static IEnumerable<string> GetFilePaths(IEnumerable<string> paths)
        {
            var result = new SortedSet<string>();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    result.UnionWith(Directory.EnumerateFiles(path, "*.blz", SearchOption.AllDirectories));
                }
                else
                    result.Add(path);   
            }

            return result;
        }
    }
}