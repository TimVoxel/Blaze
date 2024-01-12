using DPP_Compiler.Text;
using System.Collections.Immutable;
using System.Text;

namespace DPP_Compiler.Tests.CodeAnalysis
{
    internal sealed class AnnotatedText
    {
        public string Text { get; private set; }
        public ImmutableArray<TextSpan> Spans { get; private set; }

        public AnnotatedText(string text, ImmutableArray<TextSpan> spans)
        {
            Text = text;
            Spans = spans;
        }

        public static AnnotatedText Parse(string text)
        {
            text = Unindent(text);

            StringBuilder textBuilder = new StringBuilder();
            ImmutableArray<TextSpan>.Builder spanBuilder = ImmutableArray.CreateBuilder<TextSpan>();
            Stack<int> startStack = new Stack<int>();

            int position = 0;
            foreach (char c in text)
            {
                if (c == '[')
                {
                    startStack.Push(position);
                    
                }
                else if (c == ']')
                {
                    if (startStack.Count == 0)
                        throw new ArgumentException("Too many ']' in text", nameof(text));

                    int start = startStack.Pop();
                    int end = position;
                    TextSpan span = TextSpan.FromBounds(start, end);
                    spanBuilder.Add(span);
                }
                else
                {
                    position++;
                    textBuilder.Append(c);
                }
            }

            if (startStack.Count != 0)
                throw new ArgumentException("Missing ']' in text", nameof(text));

            return new AnnotatedText(textBuilder.ToString(), spanBuilder.ToImmutable());
        }

        private static string Unindent(string text)
        {
            List<string> lines = UnindentLines(text);
            return string.Join(Environment.NewLine, lines);
        }

        public static List<string> UnindentLines(string text)
        {
            List<string> lines = new List<string>();

            using (StringReader stringReader = new StringReader(text))
            {
                string? line;
                while ((line = stringReader.ReadLine()) != null)
                    lines.Add(line);
            }

            int minIndentation = int.MaxValue;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (line.Trim().Length == 0)
                {
                    lines[i] = string.Empty;
                    continue;
                }

                int indentation = line.Length - line.TrimStart().Length;
                minIndentation = Math.Min(minIndentation, indentation);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length == 0)
                    continue;

                lines[i] = lines[i].Substring(minIndentation);
            }

            while (lines.Count > 0 && lines[0].Length == 0)
                lines.RemoveAt(0);

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            return lines;
        }
    }
}
