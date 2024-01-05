using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;

namespace DPP_Compiler.Binding
{

    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly Dictionary<VariableSymbol, object?> _variables;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Binder(Dictionary<VariableSymbol, object?> variables)
        {
            _variables = variables;
        }

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
            VariableSymbol? variable = _variables.Keys.FirstOrDefault(v => v.Name == name);

            if (variable == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Span, name);
                return new BoundLiteralExpression(0);
            }
            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            BoundExpression boundExpression = BindExpression(expression.Expression);
            string name = expression.IdentifierToken.Text;

            VariableSymbol? existingVariable = _variables.Keys.FirstOrDefault(v => v.Name == name);
            if (existingVariable != null)
                _variables.Remove(existingVariable);
            VariableSymbol variable = new VariableSymbol(name, boundExpression.Type);
            _variables[variable] = null;
            return new BoundAssignmentExpression(variable, boundExpression);
        }
    }
}