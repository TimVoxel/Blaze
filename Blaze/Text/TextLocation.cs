namespace Blaze.Text
{
    public struct TextLocation
    {
        public SourceText Text { get; private set; }
        public TextSpan Span { get; private set; }

        public string FileName => Text.FileName;
        public int StartLine => Text.GetLineIndex(Span.Start);
        public int EndLine => Text.GetLineIndex(Span.End);
        public int StartCharacter => Span.Start - Text.Lines[StartLine].Start;
        public int EndCharacter => Span.End - Text.Lines[StartLine].Start;

        public TextLocation(SourceText text, TextSpan span)
        {
            Text = text;
            Span = span;
        }
    }
}
