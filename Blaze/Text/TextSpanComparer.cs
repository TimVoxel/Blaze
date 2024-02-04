namespace Blaze.Text
{
    public class TextSpanComparer : IComparer<TextSpan>
    {
        public int Compare(TextSpan x, TextSpan y)
        {
            int compare = x.Start - y.Start;
            if (compare == 0)
                compare = x.Length - y.Length;
            return compare;
        }
    }
}
