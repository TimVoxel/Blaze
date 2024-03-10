using System.Collections.Immutable;

namespace Blaze.Text
{
    public sealed class SourceText
    {
        private readonly string _text;
        public ImmutableArray<TextLine> Lines { get; private set; }
        public string FileName { get; private set; }

        private SourceText(string text, string fileName)
        {
            FileName = fileName;
            _text = text;
            Lines = ParseLines(this, text);
        }

        public char this[int index] => _text[index];
        public int Length => _text.Length;

        public int GetLineIndex(int position)
        {
            var lower = 0;
            var upper = Lines.Length - 1;

            while (lower <= upper)
            {
                var index = lower + (upper - lower) / 2;
                var start = Lines[index].Start;

                if (start == position)
                    return index;
                if (start > position)
                    upper = index - 1;
                else
                    lower = index + 1;
            }
            return lower - 1;
        }

        private ImmutableArray<TextLine> ParseLines(SourceText sourceText, string text)
        {
            var result = ImmutableArray.CreateBuilder<TextLine>();
            var lineStart = 0;
            var position = 0;

            while (position < text.Length)
            {
                var lineBreakWidth = GetLineBreakWidth(text, position);

                if (lineBreakWidth == 0)
                    position++;
                else
                {
                    AddLine(result, sourceText, position, lineStart, lineBreakWidth);
                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position >= lineStart)
                AddLine(result, sourceText, position, lineStart, 0);

            return result.ToImmutable();
        }

        private static void AddLine(ImmutableArray<TextLine>.Builder builder, SourceText sourceText, int position, int lineStart, int lineBreakWidth)
        {
            var lineLength = position - lineStart;
            var lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
            builder.Add(new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak));
        }

        private int GetLineBreakWidth(string text, int i)
        {
            var c = text[i];
            var next = i + 1 >= text.Length ? '\0' : text[i + 1];

            if (c == '\r' && next == '\n')
                return 2;
            if (c == '\r' || c == '\n')
                return 1;
            return 0;
        }

        public static SourceText From(string text, string fileName = "") => new SourceText(text, fileName);
        public override string ToString() => _text;
        public string ToString(int start, int length) => _text.Substring(start, length);
        public string ToString(TextSpan span) => ToString(span.Start, span.Length);
    }
}
