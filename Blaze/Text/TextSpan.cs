namespace Blaze.Text
{
    public struct TextSpan
    {
        public int Start { get; private set; }
        public int Length { get; private set; }

        public int End => Start + Length;

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public static TextSpan FromBounds(int start, int end) => new TextSpan(start, end - start);
        public override string ToString() => $"{Start}..{End}";

        public bool OverlapsWith(TextSpan span) => Start < span.End && End > span.Start;
    }
}
