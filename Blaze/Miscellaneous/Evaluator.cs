using Blaze.Binding;
using Blaze.Symbols;

namespace Blaze.Miscellaneuos
{
    internal class Evaluator
    {
        private readonly BoundProgram _program;
        private readonly Dictionary<VariableSymbol, object?> _globals;
        private readonly Dictionary<FunctionSymbol, BoundBlockStatement> _functions = new Dictionary<FunctionSymbol, BoundBlockStatement>();
        private readonly Stack<Dictionary<VariableSymbol, object?>> _locals = new Stack<Dictionary<VariableSymbol, object?>>();
        private Random? _random;

        private object? _lastValue;

        internal Evaluator(BoundProgram program, Dictionary<VariableSymbol, object?> variables)
        {
            _program = program;
            _globals = variables;
            _lastValue = 0;
            _locals.Push(new Dictionary<VariableSymbol, object?>());

            foreach (var functionWithBody in program.Functions)
                _functions.Add(functionWithBody.Key, functionWithBody.Value);
        }

        public object? Evaluate()
        {
            FunctionSymbol? function = _program.MainFunction;
            if (function == null)
                return null;

            BoundBlockStatement body = _functions[function];
            return EvaluateStatement(body);
        }

        private object? EvaluateStatement(BoundBlockStatement body)
        {
            Dictionary<BoundLabel, int> labelToIndex = new Dictionary<BoundLabel, int>();

            for (int i = 0; i < body.Statements.Length; i++)
                if (body.Statements[i] is BoundLabelStatement l)
                    labelToIndex.Add(l.Label, i + 1);

            for (int i = 0; i < body.Statements.Length; i++)
            {
                BoundStatement statement = body.Statements[i];

                switch (statement.Kind)
                {
                    case BoundNodeKind.NopStatement:
                        break;
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
                        object? evaluated = EvaluateExpression(conditional.Condition);
                        if (evaluated == null) break;
                        bool condition = (bool)evaluated;
                        if (condition && !conditional.JumpIfFalse || !condition && conditional.JumpIfFalse)
                            i = labelToIndex[conditional.Label] - 1;
                        break;
                    case BoundNodeKind.LabelStatement:
                        break;
                    case BoundNodeKind.ReturnStatement:
                        BoundReturnStatement returnStatement = (BoundReturnStatement)statement;
                        _lastValue = returnStatement.Expression == null ? null : EvaluateExpression(returnStatement.Expression);
                        return _lastValue;
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
            object? value = EvaluateExpression(node.Initializer);
            _lastValue = value;
            Assign(node.Variable, value);
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
            object? value = EvaluateExpression(node.Expression);
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
            if (node.Function == BuiltInFunction.Input)
                return Console.ReadLine();
            else if (node.Function == BuiltInFunction.Print)
            {
                object? message = EvaluateExpression(node.Arguments[0]);
                if (message != null)
                    Console.WriteLine(message);
                return null;
            }
            else if (node.Function == BuiltInFunction.Random)
            {
                if (_random == null)
                    _random = new Random();
                int? origin = (int?)EvaluateExpression(node.Arguments[0]);
                int? bound = (int?)EvaluateExpression(node.Arguments[1]);
                if (origin == null || bound == null)
                    return null;
                int value = _random.Next((int)origin, (int)bound);
                return value;
            }
            else
            {
                if (node.Function.Declaration == null) return null;

                Dictionary<VariableSymbol, object?> locals = new Dictionary<VariableSymbol, object?>();
                for (int i = 0; i < node.Arguments.Length; i++)
                {
                    ParameterSymbol parameter = node.Function.Parameters[i];
                    object? value = EvaluateExpression(node.Arguments[i]);
                    locals.Add(parameter, value);
                }

                _locals.Push(locals);
                BoundBlockStatement statement = _functions[node.Function];
                object? result = EvaluateStatement(statement);
                _locals.Pop();
                return result;
            }
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
            object? value = EvaluateExpression(assignment.Expression);
            Assign(assignment.Variable, value);
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
            object? operand = EvaluateExpression(unary.Operand);

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
            object? left = EvaluateExpression(binary.Left);
            object? right = EvaluateExpression(binary.Right);

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
