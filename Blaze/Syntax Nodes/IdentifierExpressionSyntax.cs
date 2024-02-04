using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class IdentifierExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IdentifierExpression;

        public IdentifierExpressionSyntax(SyntaxToken identifierToken)
        {
            IdentifierToken = identifierToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
        }
    }
}
