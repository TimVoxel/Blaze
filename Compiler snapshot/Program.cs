using Compiler_snapshot.SyntaxTokens;
using Compiler_snapshot.Syntax_Nodes;
using Compiler_snapshot.Miscellaneuos;
using Compiler_snapshot.Binding;

namespace Compiler_snapshot
{
    internal static class Program
    {
        private static void Main()
        {
            bool showTree = false;

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
                Binder binder = new Binder();
                BoundExpression boundTree = binder.BindExpression(tree.Root);

                IReadOnlyList<string> diagnostics = tree.Diagnostics.Concat(binder.Diagnostics).ToArray();

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(tree.Root);
                    Console.ResetColor();
                }

                if (diagnostics.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    foreach (string diagnostic in diagnostics)
                        Console.WriteLine(diagnostic);
                    Console.ResetColor();
                }
                else
                {
                    Evaluator evaluator = new Evaluator(boundTree);
                    Console.WriteLine(evaluator.Evaluate());
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