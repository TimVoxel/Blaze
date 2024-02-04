using Blaze;
using Blaze.Diagnostics;
using Blaze.Symbols;
using Blaze.SyntaxTokens;
using Blaze.IO;

namespace ReplExperience
{
    internal sealed class BlazeRepl : Repl
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

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            EvaluationResult result = compilation.Evaluate(_variables);
            IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;

            if (!diagnostics.Any())
            {
                _previous = compilation;
                if (result.Value != null)
                    Console.WriteLine(result.Value);
            }
            else
                Console.Out.WriteDiagnostics(result.Diagnostics, syntaxTree);
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