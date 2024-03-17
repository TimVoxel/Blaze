using Blaze.Symbols;

namespace Blaze.Binding
{
    internal static class ConstantFolding
    {
        public static BoundConstant? Fold(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            var leftConstant = left.ConstantValue;
            var rightConstant = right.ConstantValue;

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

            var leftValue = leftConstant.Value;
            var rightValue = rightConstant.Value;

            return op.OperatorKind switch 
            {
                BoundBinaryOperatorKind.Addition when left.Type == TypeSymbol.String => new BoundConstant((string)leftValue + (string)rightValue),

                BoundBinaryOperatorKind.Addition when left.Type == TypeSymbol.Int => new BoundConstant((int)leftValue + (int)rightValue),      
                BoundBinaryOperatorKind.Subtraction => new BoundConstant((int)leftValue - (int)rightValue),
                BoundBinaryOperatorKind.Multiplication => new BoundConstant((int)leftValue * (int)rightValue),
                BoundBinaryOperatorKind.Division => new BoundConstant((int)leftValue / (int)rightValue),

                
                BoundBinaryOperatorKind.LogicalMultiplication => new BoundConstant((bool)leftValue && (bool)rightValue),
                BoundBinaryOperatorKind.LogicalAddition => new BoundConstant((bool)leftValue || (bool)rightValue),

                BoundBinaryOperatorKind.Equals => new BoundConstant(Equals(leftValue, rightValue)),
                BoundBinaryOperatorKind.NotEquals => new BoundConstant(!Equals(leftValue, rightValue)),
                BoundBinaryOperatorKind.Less =>  new BoundConstant((int)leftValue < (int)rightValue),
                BoundBinaryOperatorKind.LessOrEquals => new BoundConstant((int)leftValue <= (int)rightValue),
                BoundBinaryOperatorKind.Greater =>  new BoundConstant((int)leftValue > (int)rightValue),
                BoundBinaryOperatorKind.GreaterOrEquals => new BoundConstant((int)leftValue >= (int)rightValue),

                _ => throw new Exception($"Unexpected binary operator {op.OperatorKind}"),
            };
        }

        public static BoundConstant? ComputeConstant(BoundUnaryOperator op, BoundExpression operand)
        {
            if (operand.ConstantValue == null)
                return null;

            return op.OperatorKind switch
            {
                BoundUnaryOperatorKind.Identity => new BoundConstant((int)operand.ConstantValue.Value),
                BoundUnaryOperatorKind.Negation => new BoundConstant(-(int)operand.ConstantValue.Value),
                BoundUnaryOperatorKind.LogicalNegation => new BoundConstant(!(bool)operand.ConstantValue.Value),
                _ => throw new Exception($"Unexpected unary operator {op.OperatorKind}")
            };
        }
    }
}