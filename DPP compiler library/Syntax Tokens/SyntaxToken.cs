using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.Text;

namespace DPP_Compiler.SyntaxTokens
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public override SyntaxKind Kind { get; }
        public int Position { get; private set; }
        public string Text { get; private set; }
        public object? Value { get; private set; }

        public override TextSpan Span => new TextSpan(Position, Text.Length);
        public bool IsMissingText => string.IsNullOrEmpty(Text);

        public SyntaxToken(SyntaxKind kind, int position, string? text, object? value)
        {
            Kind = kind;
            Position = position;
            Text = text ?? string.Empty;
            Value = value;
        }

        public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();
    }
}
