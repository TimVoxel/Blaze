using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler;
using DPP_Compiler.Diagnostics; 

namespace TestProgram
{
    internal static class Program
    {
        private static void Main()
        {
            bool showTree = false;
            Dictionary<VariableSymbol, object?> variables = new Dictionary<VariableSymbol, object?>();

            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    return;

                if (line == "#showTree")
                {
                    showTree = !showTree;
                    Console.WriteLine(showTree ? "Showing parse trees" : "Not showing parse trees");
                    continue;
                }

                if (line == "#clear")
                {
                    Console.Clear();
                    continue;
                }

                SyntaxTree tree = SyntaxTree.Parse(line);
                Compilation compilation = new Compilation(tree);
                EvaluationResult result = compilation.Evaluate(variables);
                IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;
                
                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(tree.Root);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.WriteLine(result.Value);
                }
                else
                {
                    foreach (Diagnostic diagnostic in diagnostics)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(diagnostic);
                        Console.ResetColor();

                        string prefix = line.Substring(0, diagnostic.Span.Start);
                        string error = line.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
                        string suffix = line.Substring(diagnostic.Span.End);

                        Console.Write("    ");
                        Console.Write(prefix);

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(error);
                        Console.ResetColor();

                        Console.Write(suffix);
                        Console.WriteLine();
                    }
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }
        }

        private static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true) 
        {
            string marker = isLast ? "└──" : "├-─";

            Console.Write(indent + marker + node.Kind);

            if (node is SyntaxToken t && t.Value != null)
                Console.Write(" " + t.Value);

            Console.WriteLine();

            indent += isLast ? "   " : "│  ";

            var last = node.GetChildren().LastOrDefault();

            foreach (SyntaxNode child in node.GetChildren())
                PrettyPrint(child, indent, child == last);
        }
    }
}