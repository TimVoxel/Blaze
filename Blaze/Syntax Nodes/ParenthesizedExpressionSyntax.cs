using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; private set; }
        public ExpressionSyntax Expression { get; private set; }
        public SyntaxToken CloseParenToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

        internal ParenthesizedExpressionSyntax(SyntaxTree tree, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken) : base(tree)
        {
            OpenParenToken = openParenToken;
            Expression = expression;
            CloseParenToken = closeParenToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return Expression;
            yield return CloseParenToken;
        }
    }
}
