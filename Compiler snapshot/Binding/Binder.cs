using Compiler_snapshot.Syntax_Nodes;

namespace Compiler_snapshot.Binding
{
    internal sealed class Binder
    {

        private List<string> _diagnostics = new List<string>();

        public IEnumerable<string> Diagnostics => _diagnostics;

        public BoundExpression BindExpression(ExpressionSyntax expression)
        {
            switch (expression.Kind)
            {
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)expression);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)expression);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)expression);
                case SyntaxKind.ParenthesizedExpression:
                    return BindExpression(((ParenthesizedExpressionSyntax)expression).Expression);
                default:
                    throw new Exception($"Unexpected syntax {expression.Kind}");
            }
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax expression)
        {
            object value = expression.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax expression)
        {
            BoundExpression boundLeft = BindExpression(expression.Left);
            BoundExpression boundRight = BindExpression(expression.Right);
            BoundBinaryOperatorKind? operatorKind = BindBinaryOperatorKind(expression.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
            if (operatorKind == null)
            {
                _diagnostics.Add($"Binary operator '{expression.OperatorToken.Text}' is not defined for types {boundLeft.Type} and {boundRight.Type}");
                return boundLeft;
            }
            return new BoundBinaryExpression(boundLeft, operatorKind.Value, boundRight);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax expression)
        {
            BoundExpression operand = BindExpression(expression.Operand);
            BoundUnaryOperatorKind? kind = BindUnaryOperatorKind(expression.OperatorToken.Kind, operand.Type);
            if (kind == null)
            {
                _diagnostics.Add($"Unary operator '{expression.OperatorToken.Text}' is not defined for type {operand.Type}");
                return operand;
            }
            return new BoundUnaryExpression(kind.Value, operand);
        }

        private BoundUnaryOperatorKind? BindUnaryOperatorKind(SyntaxKind kind, Type operandType)
        {
            if (operandType == typeof(int))
            {
                switch (kind)
                {
                    case SyntaxKind.MinusToken:
                        return BoundUnaryOperatorKind.Negation;
                    case SyntaxKind.PlusToken:
                        return BoundUnaryOperatorKind.Identity;
                }
            }

            if (operandType == typeof(bool))
            {
                switch (kind)
                {
                    case SyntaxKind.ExclamationSignToken:
                        return BoundUnaryOperatorKind.LogicalNegation;
                }
            }

            return null;
        }

        private BoundBinaryOperatorKind? BindBinaryOperatorKind(SyntaxKind kind, Type leftType, Type rightType)
        {
            if (leftType == typeof(int) && rightType == typeof(int))
            {
                switch (kind)
                {
                    case SyntaxKind.PlusToken:
                        return BoundBinaryOperatorKind.Addition;
                    case SyntaxKind.MinusToken:
                        return BoundBinaryOperatorKind.Subtraction;
                    case SyntaxKind.StarToken:
                        return BoundBinaryOperatorKind.Multiplication;
                    case SyntaxKind.SlashToken:
                        return BoundBinaryOperatorKind.Division;
                }
            }
            if (leftType == typeof(bool) && rightType == typeof(bool))
            {
                switch (kind)
                {
                    case SyntaxKind.DoubleAmpersandToken:
                        return BoundBinaryOperatorKind.LogicalMultiplication;
                    case SyntaxKind.DoublePipeToken:
                        return BoundBinaryOperatorKind.LogicalAddition;
                }
            }
            return null;

        }
    }
}