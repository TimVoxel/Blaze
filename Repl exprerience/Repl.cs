using Blaze.IO;
using System.Reflection;
using System.Text;
using TextCopy;

namespace ReplExperience
{
    internal abstract class Repl
    {
        private readonly List<MetaCommand> _metaCommands = new List<MetaCommand>();
        private readonly List<string> _submissionHistory = new List<string>();
        private int _submissionHistoryIndex;
        
        private bool _done; 
        private List<string> _document = new List<string>();

        public event Action? ShouldRender;

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
            private readonly Repl _repl;
            private readonly List<string> _submissionDocument;
            
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

            public SubmissionView(LineRendererHandler lineRenderer, Repl repl, List<string> document)
            {
                _lineRenderer = lineRenderer;
                _repl = repl;
                _submissionDocument = document;
                StartRendering();
                _cursorTop = Console.CursorTop;
                Render();
            }

            public void StartRendering()
            {
                _repl.ShouldRender += Render;
            }

            public void StopRendering()
            {
                _repl.ShouldRender -= Render;
            }

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

        private void AddToDocument(string value)
        {
            _document.Add(value);
            ShouldRender?.Invoke();
        }

        private string? EditSubmission()
        {
            _done = false;
            _document = new List<string>() { string.Empty };
            var view = new SubmissionView(RenderLine, this, _document);

            while (!_done)
            {
                var key = Console.ReadKey(true);
                HandleKey(key, view);
            }

            view.CurrentLine = _document.Count - 1;
            view.CurrentCharacter = _document[view.CurrentLine].Length;
            view.StopRendering();

            Console.WriteLine();
            return string.Join(Environment.NewLine, _document);
        }

        private void HandleKey(ConsoleKeyInfo key, SubmissionView view)
        {
            if (key.Modifiers == default)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(view);
                        break;
                    case ConsoleKey.Enter:
                        HandleEnter(view);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow(view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow(view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow(view);
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace(view);
                        break;
                    case ConsoleKey.Delete:
                        HandleDelete(view);
                        break;
                    case ConsoleKey.Tab:
                        HandleTab(view);
                        break;
                    case ConsoleKey.Oem3:
                        HandlePaste(view);
                        break;
                }
            }
            else if (key.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleControlEnter(view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleHistoryScrollUp(view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleHistoryScrollDown(view);
                        break;

                }
            }
            if (key.Key != ConsoleKey.Backspace && key.KeyChar >= ' ' && !(key.Modifiers == default && key.Key == ConsoleKey.Oem3))
                HandleTyping(view, key.KeyChar.ToString());
        }

        private void HandlePaste(SubmissionView view)
        {
            var text = ClipboardService.GetText();
            
            if (text != null)
            {
                var splitText = text.Split("\n");

                for (int i = 0; i < splitText.Length; i++)
                {
                    if (splitText[i].Length == 0)
                        continue;

                    if (splitText[i].Last() == '\n' || splitText[i].Last() == '\r')
                        splitText[i] = splitText[i].Substring(0, splitText[i].Length - 1);
                }
                _document.AddRange(splitText);

                view.CurrentLine = _document.Count - 1;
                view.CurrentCharacter = _document[view.CurrentLine].Length;
            }
            ShouldRender?.Invoke();
        }

        private void HandleHistoryScrollDown(SubmissionView view)
        {
            _submissionHistoryIndex++;
            if (_submissionHistoryIndex >= _submissionHistory.Count)
                _submissionHistoryIndex = 0;

            UpdateDocumentFromHistory(view);
        }

        private void HandleHistoryScrollUp(SubmissionView view)
        {
            _submissionHistoryIndex--;
            if (_submissionHistoryIndex < 0)
                _submissionHistoryIndex = _submissionHistory.Count - 1;
            
            UpdateDocumentFromHistory(view);
        }

        private void UpdateDocumentFromHistory(SubmissionView view)
        {
            if (_submissionHistory.Count == 0)
                return;

            _document.Clear();

            string historyItem = _submissionHistory[_submissionHistoryIndex];
            string[] lines = historyItem.Split(Environment.NewLine);
            foreach (string line in lines)
                _document.Add(line);

            ShouldRender?.Invoke();

            view.CurrentLine = _document.Count - 1;
            view.CurrentCharacter = _document[view.CurrentLine].Length;
        }

        private void HandleEscape(SubmissionView view)
        {
            _document.Clear();
            AddToDocument(string.Empty);

            view.CurrentLine = 0;
            view.CurrentCharacter = 0;
        }

        private void HandleBackspace(SubmissionView view)
        {
            int start = view.CurrentCharacter;
            if (start == 0)
            {
                if (view.CurrentLine == 0) return;

                string currentLine = _document[view.CurrentLine];
                string previous = _document[view.CurrentLine - 1];
                _document.RemoveAt(view.CurrentLine);
                view.CurrentLine--;
                _document[view.CurrentLine] = previous + currentLine;
                ShouldRender?.Invoke();
                view.CurrentCharacter = previous.Length;
            }
            else
            {
                int lineIndex = view.CurrentLine;
                string line = _document[lineIndex];
                string before = line.Substring(0, start - 1);
                string after = line.Substring(start);
                _document[lineIndex] = before + after;
                ShouldRender?.Invoke();
                view.CurrentCharacter--;
            }
        }

        private void HandleDelete(SubmissionView view)
        {
            int lineIndex = view.CurrentLine;
            string line = _document[lineIndex];
            int start = view.CurrentCharacter;
            if (start >= line.Length)
                return;

            string before = line.Substring(0, start);
            string after = line.Substring(start + 1);
            _document[lineIndex] = before + after;
            ShouldRender?.Invoke();
            view.CurrentCharacter--;
        }

        private void HandleEnter(SubmissionView view)
        {
            string submissionText = string.Join(Environment.NewLine, _document);
            if (submissionText.StartsWith("#"))
            {
                _done = true;
                return;
            }
            InsertLine(view);
        }

        private void InsertLine(SubmissionView view)
        {
            string remainer = _document[view.CurrentLine].Substring(view.CurrentCharacter);
            _document[view.CurrentLine] = _document[view.CurrentLine].Substring(0, view.CurrentCharacter);

            int lineIndex = view.CurrentLine + 1;
            _document.Insert(lineIndex, remainer);
            ShouldRender?.Invoke();
            view.CurrentCharacter = 0;
            view.CurrentLine = lineIndex;
        }

        private void HandleControlEnter(SubmissionView view)
        {
            _done = true;
        }

        private void HandleLeftArrow(SubmissionView view)
        {
            if (view.CurrentCharacter > 0)
                view.CurrentCharacter--;
        }

        private void HandleTab(SubmissionView view)
        {
            const int TabWidth = 4;
            int start = view.CurrentCharacter;
            int remaining = TabWidth - start % TabWidth;
            string line = _document[view.CurrentLine];
            _document[view.CurrentLine] = line.Insert(start, new string(' ', remaining));
            ShouldRender?.Invoke();
            view.CurrentCharacter += remaining;
        }

        private void HandleRightArrow(SubmissionView view)
        {
            string line = _document[view.CurrentLine];
            if (view.CurrentCharacter < line.Length)
                view.CurrentCharacter++;
        }

        private void HandleUpArrow(SubmissionView view)
        {
            if (view.CurrentLine > 0)
                view.CurrentLine--;
        }

        private void HandleDownArrow(SubmissionView view)
        {
            if (view.CurrentLine < _document.Count - 1)
                view.CurrentLine++;
        }

        private void HandleTyping(SubmissionView view, string text)
        {
            int lineIndex = view.CurrentLine;
            int start = view.CurrentCharacter;
            _document[lineIndex] = _document[lineIndex].Insert(start, text);
            ShouldRender?.Invoke();
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
            var args = new List<string>();
            var inQuotes = false;
            var position = 1;
            var sb = new StringBuilder();
            while (position < input.Length)
            {
                var c = input[position];
                var l = position + 1 >= input.Length ? '\0' : input[position + 1];

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

            var commandName = args.FirstOrDefault();
            if (args.Count > 0)
                args.RemoveAt(0);

            var command = _metaCommands.SingleOrDefault(mc => mc.Name == commandName);

            if (command == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: Invalid command \"{commandName}\"");
                return;
            }

            var parameters = command.Method.GetParameters();
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