using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }
        public SyntaxToken AssignmentToken { get; private set; }
        public ExpressionSyntax Expression { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

        internal AssignmentExpressionSyntax(SyntaxTree tree, SyntaxToken identifierToken, SyntaxToken assignmentToken, ExpressionSyntax expression) : base(tree)
        {
            IdentifierToken = identifierToken;
            AssignmentToken = assignmentToken;
            Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
            yield return AssignmentToken;
            yield return Expression;
        }
    }
}
