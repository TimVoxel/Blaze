namespace DPP_Compiler.Diagnostics
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
    }
}
