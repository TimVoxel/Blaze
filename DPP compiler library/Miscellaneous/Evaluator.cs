using DPP_Compiler.Binding;

namespace DPP_Compiler.Miscellaneuos
{

    internal class Evaluator
    {
        private readonly BoundExpression _root;
        private readonly Dictionary<VariableSymbol, object?> _variables;

        internal Evaluator(BoundExpression root, Dictionary<VariableSymbol, object?> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private object EvaluateExpression(BoundExpression node)
        {
            if (node is BoundLiteralExpression literal)
                return literal.Value;

            if (node is BoundVariableExpression variable)
            {
                object? value = _variables[variable.Variable];
                if (value == null)
                    throw new Exception("Unassigned variable");
                return value;
            }

            if (node is BoundAssignmentExpression assignment)
            {
                object value = EvaluateExpression(assignment.Expression);
                _variables[assignment.Variable] = value;
                return value;
            }

            if (node is BoundUnaryExpression unary)
            {
                object operand = EvaluateExpression(unary.Operand);

                switch (unary.Operator.OperatorKind)
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

                switch (binary.Operator.OperatorKind)
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
                    case BoundBinaryOperatorKind.Equals:
                        return Equals(left, right);
                    case BoundBinaryOperatorKind.NotEquals:
                        return !Equals(left, right);
                    default: throw new Exception($"Unexpected binary operator {binary.Operator.OperatorKind}");
                }
            }

            throw new Exception($"Unexpected node {node.Kind}");
        }
    }
}
