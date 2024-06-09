using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class IncrementExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }
        public SyntaxToken AssignmentToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IncrementExpression;

        internal IncrementExpressionSyntax(SyntaxTree tree, SyntaxToken identifierToken, SyntaxToken assignmentToken) : base(tree)
        {
            IdentifierToken = identifierToken;
            AssignmentToken = assignmentToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
            yield return AssignmentToken;
        }
    }
}
