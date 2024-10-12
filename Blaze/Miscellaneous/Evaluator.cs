using Blaze.Binding;
using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Miscellaneuos
{
    internal class Evaluator
    {
        private readonly BoundProgram _program;
        private readonly Dictionary<VariableSymbol, object?> _globals;
        private readonly Dictionary<FunctionSymbol, BoundStatement> _functions = new Dictionary<FunctionSymbol, BoundStatement>();
        private readonly Stack<Dictionary<VariableSymbol, object?>> _locals = new Stack<Dictionary<VariableSymbol, object?>>();
        
        private object? _lastValue;

        internal Evaluator(BoundProgram program, Dictionary<VariableSymbol, object?> variables)
        {
            _program = program;
            _globals = variables;
            _lastValue = 0;
            _locals.Push(new Dictionary<VariableSymbol, object?>());

            ///foreach (var valuePair in program.Namespaces.First().Value.Functions)
            ///    _functions.Add(valuePair.Key, valuePair.Value);
        }

        public object? Evaluate()
        {
            return null;

            //BoundStatement body = _functions[function];
            //EvaluateStatement(body);
            //return _lastValue;
        }

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
                case BoundNodeKind.DoWhileStatement:
                    EvaluateDoWhileStatement((BoundDoWhileStatement)node);
                    break;
                //HACK: Should not just ignore break and continue statements
                //      But this class shouldn't even exist so I guess it's fine
                case BoundNodeKind.BreakStatement:
                case BoundNodeKind.ContinueStatement:
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private void EvaluateDoWhileStatement(BoundDoWhileStatement node)
        {
            while (true)
            {
                EvaluateStatement(node.Body);

                var conditionValue = EvaluateExpression(node.Condition);
                if (conditionValue == null)
                    return;

                if (!(bool)conditionValue)
                    break;
            }
        }

        private void EvaluateIfStatement(BoundIfStatement node)
        {
            var conditionValue = EvaluateExpression(node.Condition);
            Debug.Assert(conditionValue != null);

            if ((bool) conditionValue)
                EvaluateStatement(node.Body);
            else if (node.ElseBody != null)
                EvaluateStatement(node.ElseBody);
        }
        
        private void EvaluateWhileStatement(BoundWhileStatement node)
        {
            while (true)
            {
                var conditionValue = EvaluateExpression(node.Condition);
                if (conditionValue == null)
                    return;

                if (!(bool)conditionValue)
                    break;

                EvaluateStatement(node.Body);
            }
        }

        private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            var value = EvaluateExpression(node.Initializer);
            _lastValue = value;
            Assign(node.Variable, value);
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement statement)
        {
            _lastValue = EvaluateExpression(statement.Expression);
        }

        private void EvaluateBlockStatement(BoundBlockStatement statement)
        {
            foreach (BoundStatement current in statement.Statements)
                EvaluateStatement(current);
        }

        private object? EvaluateExpression(BoundExpression node)
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
                case BoundNodeKind.CallExpression:
                    return EvaluateCallExpression((BoundCallExpression)node);
                case BoundNodeKind.ConversionExpression:
                    return EvaluateConversionExpression((BoundConversionExpression)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private object? EvaluateConversionExpression(BoundConversionExpression node)
        {
            var value = EvaluateExpression(node.Expression);
            if (value == null) return null;

            if (node.Type == TypeSymbol.Object)
                return value;
            if (node.Type == TypeSymbol.Bool)
                return Convert.ToBoolean(value);
            if (node.Type == TypeSymbol.Int)
                return Convert.ToInt32(value);
            if (node.Type == TypeSymbol.String)
                return Convert.ToString(value);

            throw new Exception($"Unexpected type {node.Type}");
        }

        private object? EvaluateCallExpression(BoundCallExpression node)
        {
            return null;

            /*
            if (node.Function == BuiltInFunction.RunCommand)
            {
                Console.WriteLine("Ran command:\n " + node.Arguments[0]);
                return node.Arguments[0];
            }
            else if (node.Function == BuiltInFunction.Print)
            {
                var message = EvaluateExpression(node.Arguments[0]);
                if (message != null)
                    Console.WriteLine(message);
                return null;
            }
            else if (node.Function == BuiltInFunction.Random)
            {
                if (_random == null)
                    _random = new Random();
                var origin = (int?)EvaluateExpression(node.Arguments[0]);
                var bound = (int?)EvaluateExpression(node.Arguments[1]);
                if (origin == null || bound == null)
                    return null;
                var value = _random.Next((int)origin, (int)bound);
                return value;
            }
            else
            {
                if (node.Function.Declaration == null) return null;

                var locals = new Dictionary<VariableSymbol, object?>();
                for (int i = 0; i < node.Arguments.Length; i++)
                {
                    var parameter = node.Function.Parameters[i];
                    var value = EvaluateExpression(node.Arguments[i]);
                    locals.Add(parameter, value);
                }

                _locals.Push(locals);
                var statement = _functions[node.Function];
                EvaluateStatement(statement);
                _locals.Pop();
                return _lastValue;
            }
            */
        }

        private object EvaluateLiteral(BoundLiteralExpression literal) => literal.Value;

        private object? EvaluateVariableExpression(BoundVariableExpression variable)
        {
            if (variable.Variable.Kind == SymbolKind.GlobalVariable)
                return _globals[variable.Variable];

            return _locals.Peek()[variable.Variable];
        }

        private object? EvaluateAssignmentExpression(BoundAssignmentExpression assignment)
        {
            var value = EvaluateExpression(assignment.Right);
            //Assign(assignment.Variable, value);
            return value;
        }

        private void Assign(VariableSymbol variable, object? value)
        {
            if (variable.Kind == SymbolKind.GlobalVariable)
            {
                _globals[variable] = value;
            }
            else
            {
                var locals = _locals.Peek();
                locals[variable] = value;
            }
        }

        private object? EvaluateUnaryExpression(BoundUnaryExpression unary)
        {
            var operand = EvaluateExpression(unary.Operand);

            if (operand == null)
                return null;

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

        private object? EvaluateBinaryExpression(BoundBinaryExpression binary)
        {
            var left = EvaluateExpression(binary.Left);
            var right = EvaluateExpression(binary.Right);

            if (left == null || right == null)
                return null;

            switch (binary.Operator.OperatorKind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (binary.Left.Type == TypeSymbol.Int)
                        return (int)left + (int)right;
                    else
                        return (string)left + (string)right;
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
