using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class IdentifierExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IdentifierExpression;

        internal IdentifierExpressionSyntax(SyntaxTree tree, SyntaxToken identifierToken) : base(tree)
        {
            IdentifierToken = identifierToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
        }
    }
}
