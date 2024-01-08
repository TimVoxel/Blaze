using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
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
    }
}
