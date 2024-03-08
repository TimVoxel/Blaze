using Blaze.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace ReplExperience
{
    internal abstract class Repl
    {
        private readonly List<MetaCommand> _metaCommands = new List<MetaCommand>();
        private readonly List<string> _submissionHistory = new List<string>();
        private int _submissionHistoryIndex;
        private bool _done;

        protected abstract bool IsCompleteSubmission(string text);

        protected Repl()
        {
            InitializeMetaCommands();
        }

        private void InitializeMetaCommands()
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                MetaCommandAttribute? attribute = (MetaCommandAttribute?) method.GetCustomAttribute(typeof(MetaCommandAttribute));
                if (attribute == null)
                    continue;

                MetaCommand command = new MetaCommand(attribute.Name, method, attribute.Description);
                _metaCommands.Add(command);
            }
        }

        public void Run()
        {
            while (true)
            {
                string? text = EditSubmission();
                if (string.IsNullOrEmpty(text))
                    return;

                if (!text.Contains(Environment.NewLine) && text.StartsWith("#"))
                {
                    EvaluateMetaCommand(text);
                    continue;
                }

                EvaluateSubmission(text);

                _submissionHistory.Add(text);
                _submissionHistoryIndex = 0;
            }
        }

        private sealed class SubmissionView
        {
            private readonly LineRendererHandler _lineRenderer;
            private readonly ObservableCollection<string> _submissionDocument;
            
            private int _cursorTop;
            private int _renderedLineCount;
            private int _currentLine;
            private int _currentCharacter;

            public delegate object? LineRendererHandler(IReadOnlyList<string> lines, int lineIndex, object? state);

            public int CurrentLine 
            { 
                get => _currentLine;
                set
                {
                    if (_currentLine != value)
                    {
                        _currentLine = value;
                        _currentCharacter = Math.Min(_submissionDocument[_currentLine].Length, _currentCharacter);
                        UpdateCursorPosition();
                    }
                }
            }

            public int CurrentCharacter 
            { 
                get => _currentCharacter;
                set 
                { 
                    if (_currentCharacter != value)
                    {
                        _currentCharacter = value;
                        UpdateCursorPosition();
                    }
                }
            }

            public SubmissionView(LineRendererHandler lineRenderer, ObservableCollection<string> submissionDocument)
            {
                _lineRenderer = lineRenderer;
                _submissionDocument = submissionDocument;
                _submissionDocument.CollectionChanged += SubmissionDocumentChanged;
                _cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object? sender, NotifyCollectionChangedEventArgs e) => Render();

            private void Render()
            {
                Console.CursorVisible = false;

                int lineCount = 0;
                object? state = null;

                foreach (string line in _submissionDocument)
                {
                    if (_cursorTop + lineCount >= Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, Console.WindowHeight-1);
                        Console.WriteLine();
                        if (_cursorTop > 0)
                            _cursorTop--;
                    }

                    Console.SetCursorPosition(0, _cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.Green;

                    if (lineCount == 0)
                        Console.Write("› ");
                    else
                        Console.Write("| ");

                    Console.ResetColor();
                    _lineRenderer(_submissionDocument, lineCount, state);
                    Console.Write(new string(' ', Console.WindowWidth - line.Length - 2));
                    lineCount++;
                }

                var numberOfBlankLines = _renderedLineCount - lineCount;
                if (numberOfBlankLines > 0)
                {
                    string blankLine = new string(' ', Console.WindowWidth);
                    for (int i = 0; i < numberOfBlankLines; i++)
                    {
                        Console.SetCursorPosition(0, _cursorTop + lineCount + i);
                        Console.WriteLine(blankLine);
                    }
                        
                }
                _renderedLineCount = lineCount;

                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition()
            {
                Console.CursorTop = _cursorTop + _currentLine;
                Console.CursorLeft = 2 + _currentCharacter;
            }
        }

        private string? EditSubmission()
        {
            _done = false;
            ObservableCollection<string> document = new ObservableCollection<string>() { "" };
            SubmissionView view = new SubmissionView(RenderLine, document);

            while (!_done)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                HandleKey(key, document, view);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;

            Console.WriteLine();
            return string.Join(Environment.NewLine, document);
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;
                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(document, view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow(document, view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;
                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;
                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;
                }
            }
            else if (key.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleControlEnter(document, view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleHistoryScrollUp(document, view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleHistoryScrollDown(document, view);
                        break;
                }
            }
            if (key.Key != ConsoleKey.Backspace && key.KeyChar >= ' ')
                HandleTyping(document, view, key.KeyChar.ToString());
        }

        private void HandleHistoryScrollDown(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex++;
            if (_submissionHistoryIndex >= _submissionHistory.Count)
                _submissionHistoryIndex = 0;

            UpdateDocumentFromHistory(document, view);
        }

        private void HandleHistoryScrollUp(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex--;
            if (_submissionHistoryIndex < 0)
                _submissionHistoryIndex = _submissionHistory.Count - 1;
            
            UpdateDocumentFromHistory(document, view);
        }

        private void UpdateDocumentFromHistory(ObservableCollection<string> document, SubmissionView view)
        {
            if (_submissionHistory.Count == 0)
                return;

            document.Clear();

            string historyItem = _submissionHistory[_submissionHistoryIndex];
            string[] lines = historyItem.Split(Environment.NewLine);
            foreach (string line in lines)
                document.Add(line);

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentLine = 0;
            view.CurrentCharacter = 0;
        }

        private void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            int start = view.CurrentCharacter;
            if (start == 0)
            {
                if (view.CurrentLine == 0) return;

                string currentLine = document[view.CurrentLine];
                string previous = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                view.CurrentLine--;
                document[view.CurrentLine] = previous + currentLine;
                view.CurrentCharacter = previous.Length;
            }
            else
            {
                int lineIndex = view.CurrentLine;
                string line = document[lineIndex];
                string before = line.Substring(0, start - 1);
                string after = line.Substring(start);
                document[lineIndex] = before + after;
                view.CurrentCharacter--;
            }
        }

        private void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            int lineIndex = view.CurrentLine;
            string line = document[lineIndex];
            int start = view.CurrentCharacter;
            if (start >= line.Length)
                return;

            string before = line.Substring(0, start);
            string after = line.Substring(start + 1);
            document[lineIndex] = before + after;
            view.CurrentCharacter--;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            string submissionText = string.Join(Environment.NewLine, document);
            if (submissionText.StartsWith("#") || IsCompleteSubmission(submissionText))
            {
                _done = true;
                return;
            }

            InsertLine(document, view);
        }

        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            string remainer = document[view.CurrentLine].Substring(view.CurrentCharacter);
            document[view.CurrentLine] = document[view.CurrentLine].Substring(0, view.CurrentCharacter);

            int lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remainer);
            view.CurrentCharacter = 0;
            view.CurrentLine = lineIndex;
        }

        private void HandleControlEnter(ObservableCollection<string> document, SubmissionView view)
        {
            _done = true;
        }

        private void HandleLeftArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentCharacter > 0)
                view.CurrentCharacter--;
        }

        private void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int TabWidth = 4;
            int start = view.CurrentCharacter;
            int remaining = TabWidth - start % TabWidth;
            string line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remaining));
            view.CurrentCharacter += remaining;
        }

        private void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            string line = document[view.CurrentLine];
            if (view.CurrentCharacter < line.Length)
                view.CurrentCharacter++;
        }

        private void HandleUpArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine > 0)
                view.CurrentLine--;
        }

        private void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
                view.CurrentLine++;
        }

        private void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            int lineIndex = view.CurrentLine;
            int start = view.CurrentCharacter;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentCharacter += text.Length;
        }

        protected virtual object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
        {
            Console.WriteLine(lines[lineIndex]);
            return state;
        }

        protected void ClearHistory()
        {
            _submissionHistory.Clear();
        }

        protected virtual void EvaluateMetaCommand(string input)
        {
            List<string> args = new List<string>();
            bool inQuotes = false;
            int position = 1;
            StringBuilder sb = new StringBuilder();
            while (position < input.Length)
            {
                char c = input[position];
                char l = position + 1 >= input.Length ? '\0' : input[position + 1];

                if (char.IsWhiteSpace(c))
                {
                    if (!inQuotes)
                        CommitPendingArgument();
                    else
                        sb.Append(c);
                }
                else if (c == '\"')
                {
                    if (!inQuotes)
                        inQuotes = true;
                    else if (l == '\"')
                    {
                        sb.Append(c);
                        position++;
                    }
                    else
                        inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }

                position++;
            }

            CommitPendingArgument();

            void CommitPendingArgument()
            {
                var arg = sb.ToString();
                if (!string.IsNullOrWhiteSpace(arg))
                    args.Add(arg);
                sb.Clear();
            }

            string? commandName = args.FirstOrDefault();
            if (args.Count > 0)
                args.RemoveAt(0);

            MetaCommand? command = _metaCommands.SingleOrDefault(mc => mc.Name == commandName);

            if (command == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: Invalid command \"{commandName}\"");
                return;
            }

            ParameterInfo[] parameters = command.Method.GetParameters();
            if (args.Count != parameters.Length)
            {
                string parameterNames = string.Join(", ", parameters.Select(p => $"<{p.Name}>"));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: Invalid number of arguments");
                Console.WriteLine($"usage: #{command.Name} {parameterNames}");
                return;
            }
            command.Method.Invoke(this, args.ToArray());
        }

        protected abstract void EvaluateSubmission(string text);


        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        protected sealed class MetaCommandAttribute : Attribute
        {
            public string Name { get; private set; }
            public string Description { get; set; }

            public MetaCommandAttribute(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }

        private sealed class MetaCommand
        {
            public string Name { get; private set; }
            public MethodInfo Method { get; private set; }
            public string Description { get; private set; }

            public MetaCommand(string name, MethodInfo method, string description)
            {
                Name = name;
                Method = method;
                Description = description;
            }
        }

        [MetaCommand("help", "Shows the list of all commands")]
        protected void EvaluateHelp()
        {
            int maxLength = _metaCommands.Max(mc => mc.Name.Length);

            foreach (MetaCommand command in _metaCommands.OrderBy(mc => mc.Name))
            {
                string paddedName = command.Name.PadRight(maxLength);
                Console.Out.WritePunctuation("#");
                Console.Out.WriteLabel(paddedName);
                Console.Out.WritePunctuation($"  {command.Description}");
                Console.WriteLine();
            }
        }
    }
}