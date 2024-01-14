using DPP_Compiler.Binding;
using DPP_Compiler.Symbols;
using System.Xml.Linq;

namespace DPP_Compiler.Miscellaneuos
{

    internal class Evaluator
    {
        private readonly BoundBlockStatement _root;
        private readonly Dictionary<VariableSymbol, object?> _variables;

        private object _lastValue;

        internal Evaluator(BoundBlockStatement root, Dictionary<VariableSymbol, object?> variables)
        {
            _root = root;
            _variables = variables;
            _lastValue = 0;
        }

        public object Evaluate()
        {
            Dictionary<BoundLabel, int> labelToIndex = new Dictionary<BoundLabel, int>();

            for (int i = 0; i < _root.Statements.Length; i++)
            {
                if (_root.Statements[i] is BoundLabelStatement l)
                    labelToIndex.Add(l.Label, i + 1);
            }

            for (int i = 0; i < _root.Statements.Length; i++)
            {
                BoundStatement statement = _root.Statements[i];

                switch (statement.Kind)
                {
                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement)statement);
                        break;
                    case BoundNodeKind.VariableDeclarationStatement:
                        EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)statement);
                        break;
                    case BoundNodeKind.GoToStatement:
                        BoundGotoStatement gotoStatement = (BoundGotoStatement)statement;
                        i = labelToIndex[gotoStatement.Label] - 1;
                        break;
                    case BoundNodeKind.ConditionalGotoStatement:
                        BoundConditionalGotoStatement conditional = (BoundConditionalGotoStatement)statement;
                        bool condition = (bool) EvaluateExpression(conditional.Condition);
                        if (condition && !conditional.JumpIfFalse || !condition && conditional.JumpIfFalse)
                            i = labelToIndex[conditional.Label] - 1;
                        break;
                    case BoundNodeKind.LabelStatement:
                        break;
                    default:
                        throw new Exception($"Unexpected node {statement.Kind}");
                }
            }
            
            return _lastValue;
        }

        /*
        private void EvaluateStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    EvaluateBlockStatement((BoundBlockStatement)node);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement)node);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)node);
                    break;
                case BoundNodeKind.IfStatement:
                    EvaluateIfStatement((BoundIfStatement)node);
                    break;
                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement)node);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        
        private void EvaluateIfStatement(BoundIfStatement node)
        {
            bool conditionValue = (bool) EvaluateExpression(node.Condition);
            if (conditionValue)
                EvaluateStatement(node.Body);
            else if (node.ElseBody != null)
                EvaluateStatement(node.ElseBody);
        }
        

        private void EvaluateWhileStatement(BoundWhileStatement node)
        {
            while ((bool) EvaluateExpression(node.Condition))
                EvaluateStatement(node.Body);
        }
        */

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            object value = EvaluateExpression(node.Initializer);
            _variables[node.Variable] = value;
            _lastValue = value;
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement statement)
        {
            _lastValue = EvaluateExpression(statement.Expression);
        }

        /*
        private void EvaluateBlockStatement(BoundBlockStatement statement)
        {
            foreach (BoundStatement current in statement.Statements)
                EvaluateStatement(current);
        }
        */

        private object EvaluateExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    return EvaluateLiteral((BoundLiteralExpression)node);
                case BoundNodeKind.VariableExpression:
                    return EvaluateVariableExpression((BoundVariableExpression)node);
                case BoundNodeKind.AssignmentExpression:
                    return EvaluateAssignmentExpression((BoundAssignmentExpression)node);
                case BoundNodeKind.UnaryExpression:
                    return EvaluateUnaryExpression((BoundUnaryExpression)node);
                case BoundNodeKind.BinaryExpression:
                    return EvaluateBinaryExpression((BoundBinaryExpression)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private object EvaluateLiteral(BoundLiteralExpression literal) => literal.Value;
        
        private object EvaluateVariableExpression(BoundVariableExpression variable)
        {
            object? value = _variables[variable.Variable];
            if (value == null)
                throw new Exception("Unassigned variable");
            return value;
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression assignment)
        {
            object value = EvaluateExpression(assignment.Expression);
            _variables[assignment.Variable] = value;
            return value;
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unary)
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

        private object EvaluateBinaryExpression(BoundBinaryExpression binary)
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
                case BoundBinaryOperatorKind.Less:
                    return (int)left < (int)right;
                case BoundBinaryOperatorKind.LessOrEquals:
                    return (int)left <= (int)right;
                case BoundBinaryOperatorKind.Greater:
                    return (int)left > (int)right;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return (int)left >= (int)right;
                default: throw new Exception($"Unexpected binary operator {binary.Operator.OperatorKind}");
            }
        }
    }
}
