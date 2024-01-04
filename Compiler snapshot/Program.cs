using Compiler_snapshot.SyntaxTokens;
using Compiler_snapshot.Syntax_Nodes;

namespace Compiler_snapshot
{
    internal class Program
    {
        static void Main(string[] args)
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

                ConsoleColor color = Console.ForegroundColor;
                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(tree.Root);
                    Console.ForegroundColor = color;
                }

                if (tree.Diagnostics.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    foreach (string diagnostic in tree.Diagnostics)
                        Console.WriteLine(diagnostic);
                    Console.ForegroundColor = color;
                }
                else
                {
                    Evaluator evaluator = new Evaluator(tree.Root);
                    Console.WriteLine(evaluator.Evaluate());
                }
            }
        }

        static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true) 
        {
            string marker = isLast ? "└──" : "├-─";

            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);

            if (node is SyntaxToken t && t.Value != null)
            {
                Console.Write(" ");
                Console.Write(t.Value);
            }

            Console.WriteLine();

            indent += isLast ? "    " : "│   ";

            var last = node.GetChildren().LastOrDefault();

            foreach (SyntaxNode child in node.GetChildren())
                PrettyPrint(child, indent, child == last);
        }
    }
}