namespace Blaze.Text
{
    public sealed class TextLine
    {
        public SourceText Text { get; private set; }
        public int Start { get; private set; }
        public int Length { get; private set; }
        public int LengthIncludingLineBreak { get; private set; }

        public TextSpan Span => new TextSpan(Start, Length);
        public TextSpan SpanIncludingLineBreak => new TextSpan(Start, LengthIncludingLineBreak);
        public int End => Start + Length;

        public TextLine(SourceText text, int start, int length, int lengthIncludingLineBreak)
        {
            Text = text;
            Start = start;
            Length = length;
            LengthIncludingLineBreak = lengthIncludingLineBreak;
        }

        public override string ToString() => Text.ToString(Span);
    }
}
