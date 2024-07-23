using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class SimpleNameExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.SimpleNameExpression;

        internal SimpleNameExpressionSyntax(SyntaxTree tree, SyntaxToken identifierToken) : base(tree)
        {
            IdentifierToken = identifierToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
        }
    }
}
