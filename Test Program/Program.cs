using DPP_Compiler;
using DPP_Compiler.Diagnostics;
using DPP_Compiler.Text;
using System.Text;

namespace TestProgram
{
    internal static class Program
    {
        private static void Main()
        {
            bool showTree = false;
            Dictionary<VariableSymbol, object?> variables = new Dictionary<VariableSymbol, object?>();

            StringBuilder textBuilder = new StringBuilder();

            while (true)
            {
                if (textBuilder.Length == 0)
                    Console.Write(">");
                else
                    Console.Write("|");

                string? inputLine = Console.ReadLine();
                bool isBlank = string.IsNullOrEmpty(inputLine);
              
                if (textBuilder.Length == 0)
                {
                    if (isBlank) break;
                 
                    if (inputLine == "#showTree")
                    {
                        showTree = !showTree;
                        Console.WriteLine(showTree ? "Showing parse trees" : "Not showing parse trees");
                        continue;
                    }
                    if (inputLine == "#clear")
                    {
                        Console.Clear();
                        continue;
                    }
                }

                textBuilder.AppendLine(inputLine);
                string inputText = textBuilder.ToString();

                SyntaxTree syntaxTree = SyntaxTree.Parse(inputText);

                if (!isBlank && syntaxTree.Diagnostics.Any())
                    continue;

                Compilation compilation = new Compilation(syntaxTree);
                EvaluationResult result = compilation.Evaluate(variables);
                IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;
                
                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    syntaxTree.Root.WriteTo(Console.Out);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.WriteLine(result.Value);
                }
                else
                {
                    SourceText text = syntaxTree.Text;
                    foreach (Diagnostic diagnostic in diagnostics)
                    {
                        int lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                        TextLine line = text.Lines[lineIndex];
                        int lineNumber = lineIndex + 1;
                        int character = diagnostic.Span.Start - text.Lines[lineIndex].Start + 1;

                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"Line {lineNumber}, Char {character}: ");
                        Console.WriteLine(diagnostic);
                        Console.ResetColor();

                        TextSpan prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);
                        TextSpan suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, line.End);

                        string prefix = text.ToString(prefixSpan);
                        string error = text.ToString(diagnostic.Span);
                        string suffix = text.ToString(suffixSpan);

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
                textBuilder.Clear();
            }
        }
    }
}