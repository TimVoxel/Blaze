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

            if (args.Length > 1)
            {
                Console.WriteLine("error: only one path supported for now");
            }

            string path = args.Single();
            string text = File.ReadAllText(path);

            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            Compilation compilation = new Compilation(syntaxTree);
            EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object?>());

            if (!result.Diagnostics.Any())
            {
                if (result.Value != null)
                    Console.Out.WriteLine(result.Value);
            }
            else
            {
                Console.Error.WriteDiagnostics(result.Diagnostics, syntaxTree);
            }
            Console.ReadKey();
        }
    }
}