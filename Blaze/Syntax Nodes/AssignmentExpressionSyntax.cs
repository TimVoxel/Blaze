using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{

    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Left { get; }
        public SyntaxToken AssignmentToken { get; }
        public ExpressionSyntax Right { get; }

        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

        internal AssignmentExpressionSyntax(SyntaxTree tree, ExpressionSyntax targetExpression, SyntaxToken assignmentToken, ExpressionSyntax expression) : base(tree)
        {
            Left = targetExpression;
            AssignmentToken = assignmentToken;
            Right = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return AssignmentToken;
            yield return Right;
        }
    }
}
