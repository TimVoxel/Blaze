using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;
using System.Collections.Immutable;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableArray<VariableSymbol> Variables { get; private set; }
        public BoundStatement Statement { get; private set; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, BoundStatement statement)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Statement = statement;
        }
    }

    internal sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new Dictionary<string, VariableSymbol>();

        public BoundScope? Parent { get; private set; }

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public bool TryDeclare(VariableSymbol variable)
        {
            if (_variables.ContainsKey(variable.Name)) return false;
            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookup(string name, out VariableSymbol? variable)
        {
            if (_variables.TryGetValue(name, out variable))
                return true;

            if (Parent == null)
                return false;

            return Parent.TryLookup(name, out variable);
        }
    
        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _variables.Values.ToImmutableArray();
    }


    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly BoundScope _scope;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Binder(BoundScope? parent)
        {
            _scope = new BoundScope(parent);
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            BoundScope? parentScope = CreateParentScopes(previous);
            Binder binder = new Binder(parentScope);
            BoundStatement statement = binder.BindStatement(syntax.Statement);
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();
            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, variables, statement);
        }

        private static BoundScope? CreateParentScopes(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope? parent = null;
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new BoundScope(parent);
                foreach (VariableSymbol variable in previous.Variables)
                    scope.TryDeclare(variable);

                parent = scope;
            }
            return parent;
        }

        private BoundStatement BindStatement(StatementSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.BlockStatement:
                    return BindBlockStatement((BlockStatementSyntax)syntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            BoundExpression boundExpression = BindExpression(syntax.Expression);
            return new BoundExpressionStatement(boundExpression);
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            ImmutableArray<BoundStatement>.Builder boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();
            
            foreach (StatementSyntax statement in syntax.Statements)
            {
                BoundStatement boundStatement = BindStatement(statement);
                boundStatements.Add(boundStatement);
            }
            return new BoundBlockStatement(boundStatements.ToImmutable());
        }

        private BoundExpression BindExpression(ExpressionSyntax expression)
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
                case SyntaxKind.IdentifierExpression:
                    return BindIdentifierExpression((IdentifierExpressionSyntax)expression);
                case SyntaxKind.AssignmentExpression:
                    return BindAssignmentExpression((AssignmentExpressionSyntax)expression);
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
            BoundBinaryOperator? op = BoundBinaryOperator.Bind(expression.OperatorToken.Kind, boundLeft.Type, boundRight.Type);
                
            if (op == null)
            {
                _diagnostics.ReportUndefinedBinaryOperator(expression.OperatorToken.Span, expression.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return boundLeft;
            }
            return new BoundBinaryExpression(boundLeft, op, boundRight);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax expression)
        {
            BoundExpression operand = BindExpression(expression.Operand);
            BoundUnaryOperator? op = BoundUnaryOperator.Bind(expression.OperatorToken.Kind, operand.Type); 
            if (op == null)
            {
                _diagnostics.ReportUndefinedUnaryOperator(expression.OperatorToken.Span, expression.OperatorToken.Text, operand.Type);
                return operand;
            }
            return new BoundUnaryExpression(op, operand);
        }

        private BoundExpression BindIdentifierExpression(IdentifierExpressionSyntax expression)
        {
            string name = expression.IdentifierToken.Text;
            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Span, name);
                return new BoundLiteralExpression(0);
            }
            return (variable == null) ? new BoundLiteralExpression(0) : new BoundVariableExpression(variable);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            BoundExpression boundExpression = BindExpression(expression.Expression);
            string name = expression.IdentifierToken.Text;
            
            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                variable = new VariableSymbol(name, boundExpression.Type);
                _scope.TryDeclare(variable);
            }
            if (variable != null)
            {
                if (boundExpression.Type != variable.Type)
                {
                    _diagnostics.ReportCannotConvert(expression.Expression.Span, boundExpression.Type, variable.Type);
                    return boundExpression;
                }
                return new BoundAssignmentExpression(variable, boundExpression);
            }
            throw new Exception("Somehow the variable is null");
        }
    }
}