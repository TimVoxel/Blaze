using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class UnaryExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OperatorToken { get; private set; }
        public ExpressionSyntax Operand { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.UnaryExpression;

        public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
        {
            OperatorToken = operatorToken;
            Operand = operand;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OperatorToken;
            yield return Operand;
        }
    }
}
