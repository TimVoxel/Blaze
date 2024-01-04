using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot.Syntax_Nodes
{
    public sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Left { get; private set; }
        public SyntaxToken OperatorToken { get; private set; }
        public ExpressionSyntax Right { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

        public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
        {
            Left = left;
            OperatorToken = operatorToken;
            Right = right;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }
}
