using Compiler_snapshot.Syntax_Nodes;

namespace Compiler_snapshot.SyntaxTokens
{
    public class SyntaxToken : SyntaxNode
    {
        public override SyntaxKind Kind { get; }
        public int Position { get; private set; }
        public string? Text { get; private set; }
        public object? Value { get; private set; }

        public SyntaxToken(SyntaxKind kind, int position, string? text, object? value)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
        }

        public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();
    }

    public enum SyntaxKind 
    {
        Incorrect,
        EndOfFile,
        WhiteSpace,
        IntegerLiteral,
        Plus,
        Minus,
        Star,
        Slash,
        OpenParen,
        CloseParen,
        BinaryExpression,
        ParenthesizedExpression
    }
}
