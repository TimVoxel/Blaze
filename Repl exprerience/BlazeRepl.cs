using Blaze;
using Blaze.Symbols;
using Blaze.IO;
using Blaze.Classification;

namespace ReplExperience
{
    internal sealed class BlazeRepl : Repl
    {
        private static bool _loadingSubmissions;
        private Compilation? _previous;
        private bool _showTree;
        private bool _showProgram;
        private Dictionary<VariableSymbol, object?> _variables = new Dictionary<VariableSymbol, object?>();
        
        private static string SubmissionsDirectory
        {
            get
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "Blaze", "Submissions");
            }
        }

        public BlazeRepl()
        {
            LoadSubmissions();
        }

        protected override bool IsCompleteSubmission(string text)
        {
            //This used to be viable but no longer works
            //In case you want to have multiple namespaces

            /*
            if (string.IsNullOrEmpty(text))
                return false;

            SyntaxTree syntaxTree = SyntaxTree.Parse(text);

            MemberSyntax? lastMember = syntaxTree.Root.Namespaces.LastOrDefault();
            if (lastMember == null) 
                return false;

            return !lastMember.GetLastToken().IsMissingText;*/
            return false;
        }

        protected override object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
        {
            SyntaxTree tree;

            if (state == null)
            {
                var text = string.Join(Environment.NewLine, lines);
                if (string.IsNullOrEmpty(text))
                    text = " ";
                tree = SyntaxTree.Parse(text);
            }
            else
                tree = (SyntaxTree) state;

            var lineSpan = tree.Text.Lines[lineIndex].Span;
            var classifiedSpans = Classifier.Classify(tree, lineSpan);

            foreach (var classifiedSpan in classifiedSpans)
            {
                var tokenText = tree.Text.ToString(classifiedSpan.Span);

                Console.ForegroundColor = classifiedSpan.Classification switch
                {
                    Classification.Number       => ConsoleColor.Yellow,
                    Classification.Identifier   => ConsoleColor.Cyan,
                    Classification.String       => ConsoleColor.DarkYellow,
                    Classification.Comment      => ConsoleColor.DarkGreen,
                    Classification.Keyword      => ConsoleColor.DarkCyan,
                    Classification.Text         => ConsoleColor.Gray,
                    _                           => ConsoleColor.Gray
                };

                Console.Write(tokenText);
                Console.ResetColor();
            }

            return tree;
        }

        protected override void EvaluateSubmission(string text)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = Compilation.CreateScript(syntaxTree);

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            var result = compilation.Evaluate(_variables);
            var diagnostics = result.Diagnostics;

            if (!diagnostics.Any())
            {
                _previous = compilation;
                if (result.Value != null)
                    Console.WriteLine(result.Value);

                SaveSubmission(text);
            }
            else
                Console.Out.WriteDiagnostics(result.Diagnostics);
        }

        private void LoadSubmissions()
        {
            var submissionsDirectory = SubmissionsDirectory;
            if (!Directory.Exists(submissionsDirectory))
                return;

            var files = Directory.GetFiles(submissionsDirectory);
            if (files.Length == 0)
                return;

            _loadingSubmissions = true;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Loaded {files.Length} submission(s)");
            Console.ResetColor();

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                EvaluateSubmission(text); 
            }

            _loadingSubmissions = false;
        }

        private static void ClearSubmissions()
        {
            var submissionsDirectory = SubmissionsDirectory;
            if (Directory.Exists(submissionsDirectory))
                Directory.Delete(submissionsDirectory, true);
        }

        private static void SaveSubmission(string text)
        {
            if (_loadingSubmissions)
                return;

            var submissionsDirectory = SubmissionsDirectory;
            Directory.CreateDirectory(submissionsDirectory);
            var count = Directory.GetFiles(submissionsDirectory).Length;
            var name = $"submission{count:0000}";
            var fileName = Path.Combine(submissionsDirectory, name);
            File.WriteAllText(fileName, text);
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

        [MetaCommand("load", "Loads a script file")]
        private void EvaluateScriptLoad(string path)
        {
            path = Path.GetFullPath(path);

            if (!File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"File {path} does not exist");
                Console.ResetColor();
                return;
            }

            string text = File.ReadAllText(path);
            EvaluateSubmission(text);
        }

        [MetaCommand("listSymbols", "Lists all defined symbols")]
        private void EvaluateLs()
        {
            if (_previous == null) return;
            var symbols = _previous.GetSymbols().OrderBy(s => s.Kind).ThenBy(s => s.Name);

            foreach (Symbol symbol in symbols)
            {
                symbol.WriteTo(Console.Out);
                Console.WriteLine();
            }
        }

        [MetaCommand("deleteSubmissions", "Deletes all saved submissions")]
        private void EvaluateDeleteSubmissions() => ClearSubmissions();


        [MetaCommand("dump", "Shows the bound tree of a given function")]
        private void EvaluateDump(string functionName)
        {
            if (_previous == null)
                return;

            FunctionSymbol? function = _previous.GetSymbols().OfType<FunctionSymbol>().LastOrDefault(f => f.Name == functionName);
            if (function == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: Function \"{functionName}\" does not exist");
                Console.ResetColor();
                return;
            }

            _previous.EmitTree(function, Console.Out);
        }

    }
}