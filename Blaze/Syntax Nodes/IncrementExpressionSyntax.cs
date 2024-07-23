using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class IncrementExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Expression { get; private set; }
        public SyntaxToken AssignmentToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IncrementExpression;

        internal IncrementExpressionSyntax(SyntaxTree tree, ExpressionSyntax operand, SyntaxToken assignmentToken) : base(tree)
        {
            Expression = operand;
            AssignmentToken = assignmentToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return AssignmentToken;
        }
    }
}
