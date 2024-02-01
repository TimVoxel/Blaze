using DPP_Compiler;
using DPP_Compiler.Diagnostics;
using DPP_Compiler.Symbols;
using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Text;

namespace ReplExperience
{
    internal sealed class DPPRepl : Repl
    {
        private Compilation? _previous;
        private bool _showTree;
        private bool _showProgram;
        private Dictionary<VariableSymbol, object?> _variables = new Dictionary<VariableSymbol, object?>();

        protected override bool IsCompleteSubmission(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            return !syntaxTree.Root.Members.Last().GetLastToken().IsMissingText;
        }

        protected override void RenderLine(string line)
        {
            IEnumerable<SyntaxToken> tokens = SyntaxTree.ParseTokens(line);
            foreach (SyntaxToken token in tokens)
            {
                Console.ForegroundColor = token.GetConsoleColor();
                Console.Write(token.Text);
                Console.ResetColor();
            }
        }

        protected override void EvaluateSubmission(string inputText)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(inputText);
            Compilation compilation = (_previous == null) ? new Compilation(syntaxTree) : _previous.ContinueWith(syntaxTree);
            EvaluationResult result = compilation.Evaluate(_variables);
            IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            if (!diagnostics.Any())
            {
                _previous = compilation;
                if (result.Value != null)
                    Console.WriteLine(result.Value);
            }
            else
            {
                SourceText text = syntaxTree.Text;
                foreach (Diagnostic diagnostic in diagnostics.OrderBy(d => d.Span, new TextSpanComparer()))
                {
                    int lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                    TextLine line = text.Lines[lineIndex];
                    int lineNumber = lineIndex + 1;
                    int character = diagnostic.Span.Start - text.Lines[lineIndex].Start + 1;

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
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
        }

        protected override void EvaluateMetaCommand(string inputLine)
        {
            if (inputLine == "#showTree")
            {
                _showTree = !_showTree;
                Console.WriteLine(_showTree ? "Showing parse trees" : "Not showing parse trees");
            }
            else if (inputLine == "#showProgram")
            {
                _showProgram = !_showProgram;
                Console.WriteLine(_showProgram ? "Showing bound trees" : "Not showing bound trees");
            }
            else if (inputLine == "#clear")
            {
                Console.Clear();
            }
            else if (inputLine == "#reset")
            {
                _previous = null;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid command {inputLine}");
            }
        }
    }
}