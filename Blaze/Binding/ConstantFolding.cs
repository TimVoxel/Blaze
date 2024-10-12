using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Binding
{
    internal static class ConstantFolding
    {
        public static BoundConstant? Fold(BoundExpression leftExpression, BoundBinaryOperator op, BoundExpression rightExpression)
        {
            var leftConstant = leftExpression.ConstantValue;
            var rightConstant = rightExpression.ConstantValue;

            if (op.OperatorKind == BoundBinaryOperatorKind.LogicalMultiplication)
            {
                if (leftConstant != null && !(bool)leftConstant.Value ||
                    rightConstant != null && !(bool)rightConstant.Value)
                    return new BoundConstant(false);
            }
            else if (op.OperatorKind == BoundBinaryOperatorKind.LogicalAddition)
            {
                if (leftConstant != null && (bool)leftConstant.Value ||
                    rightConstant != null && (bool)rightConstant.Value)
                    return new BoundConstant(true);
            }

            if (leftConstant == null || rightConstant == null)
                return null;

            var left = leftConstant.Value;
            var right = rightConstant.Value;

            switch (op.OperatorKind)
            {
                case BoundBinaryOperatorKind.Addition:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left + (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left + (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left + (double)right);
                    else if (leftExpression.Type == TypeSymbol.String)
                        return new BoundConstant((string)left + (string)right);
                    break;
                case BoundBinaryOperatorKind.Subtraction:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left - (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left - (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left - (double)right);
                    break;
                case BoundBinaryOperatorKind.Multiplication:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left * (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left * (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left * (double)right);
                    break;
                case BoundBinaryOperatorKind.Division:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left / (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left / (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left / (double)right);
                    break;
                case BoundBinaryOperatorKind.Equals:
                    return new BoundConstant(Equals(left, right));
                case BoundBinaryOperatorKind.NotEquals:
                    return new BoundConstant(!Equals(left, right));
                case BoundBinaryOperatorKind.Less:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left < (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left < (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left < (double)right);
                    break;

                case BoundBinaryOperatorKind.LessOrEquals:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left <= (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left <= (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left <= (double)right);
                    break;
                case BoundBinaryOperatorKind.Greater:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left > (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left > (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left > (double)right);
                    break;
                case BoundBinaryOperatorKind.GreaterOrEquals:

                    if (leftExpression.Type == TypeSymbol.Int)
                        return new BoundConstant((int)left >= (int)right);
                    else if (leftExpression.Type == TypeSymbol.Float)
                        return new BoundConstant((float)left >= (float)right);
                    else if (leftExpression.Type == TypeSymbol.Double)
                        return new BoundConstant((double)left >= (double)right);
                    break;
            }
            throw new Exception($"Unexpected binary operator {op.OperatorKind} with types {leftExpression.Type} and {rightExpression.Type}");
        }

        public static BoundConstant? ComputeConstant(BoundUnaryOperator op, BoundExpression operand)
        {
            if (operand.ConstantValue == null)
                return null;

            switch (op.OperatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    if (operand.Type == TypeSymbol.Int)
                        return new BoundConstant((int)operand.ConstantValue.Value);
                    else if (operand.Type == TypeSymbol.Float)
                        return new BoundConstant((float)operand.ConstantValue.Value);
                    else if (operand.Type == TypeSymbol.Double)
                        return new BoundConstant((double)operand.ConstantValue.Value);
                    break;
                case BoundUnaryOperatorKind.Negation:
                    if (operand.Type == TypeSymbol.Int)
                        return new BoundConstant(-(int)operand.ConstantValue.Value);
                    else if (operand.Type == TypeSymbol.Float)
                        return new BoundConstant(-(float)operand.ConstantValue.Value);
                    else if (operand.Type == TypeSymbol.Double)
                        return new BoundConstant(-(double)operand.ConstantValue.Value);
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:
                    return new BoundConstant(!(bool)operand.ConstantValue.Value);
                default:
                    throw new Exception($"Unexpected unary operator {op.OperatorKind}");
            };
            return null;
        }

        public static BoundConstant? ComputeCast(BoundExpression expression, TypeSymbol type)
        {
            var constant = expression.ConstantValue;
            if (constant == null)
                return null;

            if (type == TypeSymbol.String)
            {
                string? value = constant.Value.ToString();
                Debug.Assert(value != null);
                return new BoundConstant(value);
            }

            if (type == TypeSymbol.Float)
                return new BoundConstant(Convert.ToSingle(constant.Value));
            if (type == TypeSymbol.Int)
                return new BoundConstant(Convert.ToInt32(constant.Value));
            if (type == TypeSymbol.Double)
                return new BoundConstant(Convert.ToDouble(constant.Value));
            if (type == TypeSymbol.Bool)
                return new BoundConstant(Convert.ToBoolean(constant.Value));

            return null;
        }
    }
}