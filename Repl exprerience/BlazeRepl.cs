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
            if (line.StartsWith("#"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(line);
                Console.ResetColor();
                return;
            }

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
                Console.Out.WriteDiagnostics(result.Diagnostics);
        }

        [MetaCommand("showTree", "Shows/Hides the parse tree")]
        private void EvaluateShowTree()
        {
            _showTree = !_showTree;
            Console.WriteLine(_showTree ? "Showing parse trees" : "Not showing parse trees");
        }


        [MetaCommand("showProgram", "Shows/Hides the bound program")]
        private void EvaluateShowProgram()
        {
            _showProgram = !_showProgram;
            Console.WriteLine(_showProgram ? "Showing bound trees" : "Not showing bound trees");
        }

        [MetaCommand("clear", "Clears the console")]
        private void EvaluateClear()
        {
            Console.Clear();
        }


        [MetaCommand("reset", "Clears all previous submissions")]
        private void EvaluateReset()
        {
            _previous = null;
        }
    }
}