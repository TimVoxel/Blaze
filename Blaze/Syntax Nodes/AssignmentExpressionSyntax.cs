using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }
        public SyntaxToken EqualsToken { get; private set; }
        public ExpressionSyntax Expression { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

        public AssignmentExpressionSyntax(SyntaxTree tree, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression) : base(tree)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
            yield return EqualsToken;
            yield return Expression;
        }
    }
}
