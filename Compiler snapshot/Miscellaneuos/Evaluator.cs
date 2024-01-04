using Compiler_snapshot.Binding;

namespace Compiler_snapshot.Miscellaneuos
{
    internal class Evaluator
    {
        private readonly BoundExpression _root;

        internal Evaluator(BoundExpression root)
        {
            _root = root;
        }

        public object Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private object EvaluateExpression(BoundExpression node)
        {
            if (node is BoundLiteralExpression literal)
                return literal.Value;

            if (node is BoundUnaryExpression unary)
            {
                object operand = EvaluateExpression(unary.Operand);

                switch (unary.OperatorKind)
                {
                    case BoundUnaryOperatorKind.Identity:
                        return (int)operand;
                    case BoundUnaryOperatorKind.Negation:
                        return -(int)operand;
                    case BoundUnaryOperatorKind.LogicalNegation:
                        return !(bool)operand;
                    default:
                        throw new Exception($"Unexpected unary operator {unary.Kind}");
                }   
            }

            if (node is BoundBinaryExpression binary)
            {
                object left = EvaluateExpression(binary.Left);
                object right = EvaluateExpression(binary.Right);

                switch (binary.OperatorKind)
                {
                    case BoundBinaryOperatorKind.Addition:
                        return (int)left + (int)right;
                    case BoundBinaryOperatorKind.Subtraction:
                        return (int)left - (int)right;
                    case BoundBinaryOperatorKind.Multiplication:
                        return (int)left * (int)right;
                    case BoundBinaryOperatorKind.Division:
                        return (int)left / (int)right;
                    case BoundBinaryOperatorKind.LogicalMultiplication:
                        return (bool)left && (bool)right;
                    case BoundBinaryOperatorKind.LogicalAddition:
                        return (bool)left || (bool)right;
                    default: throw new Exception($"Unexpected binary operator {binary.OperatorKind}");
                }
            }

            throw new Exception($"Unexpected node {node.Kind}");
        }
    }
}
