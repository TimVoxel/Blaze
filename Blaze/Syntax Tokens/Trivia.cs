using Blaze.Text;

namespace Blaze.SyntaxTokens
{
    public sealed class Trivia
    {
        public SyntaxTree SyntaxTree { get; private set; }
        public SyntaxKind Kind { get; private set; }
        public int Position { get; private set; }
        public string? Text { get; private set; }
        public TextSpan Span => new TextSpan(Position, Text?.Length ?? 0);

        public Trivia(SyntaxTree tree, SyntaxKind kind, int position, string? text)
        {
            SyntaxTree = tree;
            Kind = kind;
            Position = position;
            Text = text;
        }
    }
}
