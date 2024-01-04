using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot.Syntax_Nodes
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }
        public abstract IEnumerable<SyntaxNode> GetChildren();
    }

    public abstract class ExpressionSyntax : SyntaxNode
    {

    }

    public sealed class NumberExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Token { get; private set; }

        public NumberExpressionSyntax(SyntaxToken numberToken)
        {
            Token = numberToken;
        }

        public override SyntaxKind Kind => SyntaxKind.IntegerLiteral;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Token;
        }
    }

    public sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Left { get; private set; }
        public SyntaxToken Operator { get; private set; }
        public ExpressionSyntax Right { get; private set; }

        public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
        {
            Left = left;
            Operator = operatorToken;
            Right = right;
        }

        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return Operator;
            yield return Right;
        }
    }

    public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; private set; }
        public ExpressionSyntax Expression { get; private set; }
        public SyntaxToken CloseParenToken { get; private set; }

        public ParenthesizedExpressionSyntax(SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken)
        {
            OpenParenToken = openParenToken;
            Expression = expression;
            CloseParenToken = closeParenToken;
        }

        public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return Expression;
            yield return CloseParenToken;
        }
    }

}
