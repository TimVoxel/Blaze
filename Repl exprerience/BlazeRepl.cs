﻿using Blaze;
using Blaze.Diagnostics;
using Blaze.Symbols;
using Blaze.SyntaxTokens;
using Blaze.IO;
using Blaze.Syntax_Nodes;
using Blaze.Text;
using System.Collections.Immutable;

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
            if (string.IsNullOrEmpty(text))
                return false;

            SyntaxTree syntaxTree = SyntaxTree.Parse(text);

            MemberSyntax? lastMember = syntaxTree.Root.Members.LastOrDefault();
            if (lastMember == null) 
                return false;

            return !lastMember.GetLastToken().IsMissingText;
        }

        private sealed class RenderState
        {
            public SourceText Text { get; private set; }
            public ImmutableArray<SyntaxToken> Tokens { get; private set; }

            public RenderState(SourceText text, ImmutableArray<SyntaxToken> tokens)
            {
                Text = text;
                Tokens = tokens;
            }
        }

        protected override object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
        {
            RenderState renderState;

            if (state == null)
            {
                var text = string.Join(Environment.NewLine, lines);
                SourceText sourceText = SourceText.From(text);
                ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(sourceText);
                renderState = new RenderState(sourceText, tokens);
                state = renderState;
            }
            else
                renderState = (RenderState) state;

            TextSpan lineSpan = renderState.Text.Lines[lineIndex].Span;

            foreach (SyntaxToken token in renderState.Tokens)
            {
                if (!lineSpan.OverlapsWith(token.Span))
                    continue;

                int tokenStart = Math.Max(token.Span.Start, lineSpan.Start);
                int tokenEnd = Math.Min(token.Span.End, lineSpan.End);
                TextSpan tokenSpan = TextSpan.FromBounds(tokenStart, tokenEnd);
                string tokenText = renderState.Text.ToString(tokenSpan);

                Console.ForegroundColor = token.Kind switch
                {
                    SyntaxKind.IntegerLiteralToken => ConsoleColor.Yellow,
                    SyntaxKind.IdentifierToken => ConsoleColor.Cyan,
                    SyntaxKind.StringLiteralToken => ConsoleColor.DarkYellow,
                    SyntaxKind.SingleLineCommentToken => ConsoleColor.DarkGreen,
                    SyntaxKind.MultiLineCommentToken => ConsoleColor.DarkGreen,
                    _ when SyntaxFacts.IsKeyword(token.Kind) => ConsoleColor.DarkCyan,
                    _ => ConsoleColor.Gray
                };

                Console.Write(tokenText);
                Console.ResetColor();
            }

            return state;
        }

        protected override void EvaluateSubmission(string text)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(text);
            Compilation compilation = Compilation.CreateScript(_previous, syntaxTree);

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

                SaveSubmission(text);
            }
            else
                Console.Out.WriteDiagnostics(result.Diagnostics);
        }

        private void LoadSubmissions()
        {
            string submissionsDirectory = SubmissionsDirectory;
            if (!Directory.Exists(submissionsDirectory))
                return;

            string[] files = Directory.GetFiles(submissionsDirectory);
            if (files.Length == 0)
                return;

            _loadingSubmissions = true;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Loaded {files.Length} submission(s)");
            Console.ResetColor();

            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                EvaluateSubmission(text); 
            }

            _loadingSubmissions = false;
        }

        private static void ClearSubmissions()
        {
            string submissionsDirectory = SubmissionsDirectory;
            if (Directory.Exists(submissionsDirectory))
                Directory.Delete(submissionsDirectory, true);
        }

        private static void SaveSubmission(string text)
        {
            if (_loadingSubmissions)
                return;

            string submissionsDirectory = SubmissionsDirectory;
            Directory.CreateDirectory(submissionsDirectory);
            int count = Directory.GetFiles(submissionsDirectory).Length;
            string name = $"submission{count:0000}";
            string fileName = Path.Combine(submissionsDirectory, name);
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
            IEnumerable<Symbol> symbols = _previous.GetSymbols().OrderBy(s => s.Kind).ThenBy(s => s.Name);
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

            FunctionSymbol? function = _previous.GetSymbols().OfType<FunctionSymbol>().SingleOrDefault(f => f.Name == functionName);
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