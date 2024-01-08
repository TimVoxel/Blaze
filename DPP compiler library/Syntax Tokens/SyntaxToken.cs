using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;

namespace DPP_Compiler.SyntaxTokens
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public override SyntaxKind Kind { get; }
        public int Position { get; private set; }
        public string Text { get; private set; }
        public object? Value { get; private set; }

        public TextSpan Span => new TextSpan(Position, Text.Length);

        public SyntaxToken(SyntaxKind kind, int position, string text, object? value)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
        }
    }
}
