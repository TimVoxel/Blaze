using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class LiteralExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken LiteralToken { get; private set; }
        public object? Value { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

        internal LiteralExpressionSyntax(SyntaxTree tree, SyntaxToken literalToken, object? value) : base(tree)
        {
            LiteralToken = literalToken;
            Value = value;
        }

        internal LiteralExpressionSyntax(SyntaxTree tree, SyntaxToken literalToken) : this(tree, literalToken, literalToken.Value) { }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
        }
    }

}
