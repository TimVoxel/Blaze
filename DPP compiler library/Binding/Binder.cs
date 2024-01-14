﻿using DPP_Compiler.Diagnostics;
using DPP_Compiler.Symbols;
using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;
using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;

namespace DPP_Compiler.Binding
{
    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private BoundScope _scope;

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
                case SyntaxKind.VariableDeclarationStatement:
                    return BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax);
                case SyntaxKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax)syntax);
                case SyntaxKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax)syntax);
                case SyntaxKind.ForStatement:
                    return BindForStatement((ForStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            BoundExpression initializer = BindExpression(syntax.Initializer);

            /*
            if (initializer.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportVoidDeclaration(syntax.Span, syntax.Identifier.Text);
                
            }
            */
            VariableSymbol variable = BindVariable(syntax.Identifier, initializer.Type);
            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            BoundExpression boundExpression = BindExpression(syntax.Expression);
            return new BoundExpressionStatement(boundExpression);
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            BoundExpression boundCondition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            BoundStatement body = BindStatement(syntax.Body);
            BoundStatement? elseBody = (syntax.ElseClause == null) ? null : BindStatement(syntax.ElseClause.Body);
            return new BoundIfStatement(boundCondition, body, elseBody);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            BoundExpression boundCondition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            BoundStatement body = BindStatement(syntax.Body);
            return new BoundWhileStatement(boundCondition, body);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            BoundExpression lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int);
            BoundExpression upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int);

            BoundScope previous = _scope;
            _scope = new BoundScope(previous);

            VariableSymbol variable = BindVariable(syntax.Identifier, TypeSymbol.Int);
            BoundStatement body = BindStatement(syntax.Body);

            _scope = previous;
            
            return new BoundForStatement(variable, lowerBound, upperBound, body);
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            ImmutableArray<BoundStatement>.Builder boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();

            BoundScope previous = _scope;
            _scope = new BoundScope(previous);

            foreach (StatementSyntax statement in syntax.Statements)
            {
                BoundStatement boundStatement = BindStatement(statement);
                boundStatements.Add(boundStatement);
            }

            _scope = previous;

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
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax)expression);
                default:
                    throw new Exception($"Unexpected syntax {expression.Kind}");
            }
        }

        private BoundExpression BindExpression(ExpressionSyntax expression, TypeSymbol desiredType)
        {
            BoundExpression boundExpression = BindExpression(expression);
            if (!boundExpression.Type.IsError && !desiredType.IsError && boundExpression.Type != desiredType)
                _diagnostics.ReportCannotConvert(expression.Span, boundExpression.Type, desiredType);
            
            return boundExpression;
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

            if (boundLeft.Type.IsError || boundRight.Type.IsError)
                return new BoundErrorExpression();

            BoundBinaryOperator? op = BoundBinaryOperator.Bind(expression.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (op == null)
            {
                _diagnostics.ReportUndefinedBinaryOperator(expression.OperatorToken.Span, expression.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression();
            }
            return new BoundBinaryExpression(boundLeft, op, boundRight);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax expression)
        {
            BoundExpression operand = BindExpression(expression.Operand);
            if (operand.Type.IsError)
                return new BoundErrorExpression();

            BoundUnaryOperator? op = BoundUnaryOperator.Bind(expression.OperatorToken.Kind, operand.Type);

            if (op == null)
            {
                _diagnostics.ReportUndefinedUnaryOperator(expression.OperatorToken.Span, expression.OperatorToken.Text, operand.Type);
                return new BoundErrorExpression();
            }
            return new BoundUnaryExpression(op, operand);
        }

        private BoundExpression BindIdentifierExpression(IdentifierExpressionSyntax expression)
        {
            string name = expression.IdentifierToken.Text;
            if (expression.IdentifierToken.IsMissingText)
                return new BoundErrorExpression();

            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Span, name);
                return new BoundErrorExpression();
            }
            return (variable == null) ? new BoundErrorExpression() : new BoundVariableExpression(variable);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax expression)
        {
            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (ExpressionSyntax argument in expression.Arguments)
                boundArguments.Add(BindExpression(argument));

            IEnumerable<FunctionSymbol> functions = BuiltInFunction.GetAll();
            FunctionSymbol? function = functions.FirstOrDefault(f => f.Name == expression.Identifier.Text);
            if (function == null)
            {
                _diagnostics.ReportUndefinedFunction(expression.Identifier.Span, expression.Identifier.Text);
                return new BoundErrorExpression();
            }
            if (function.Parameters.Length != expression.Arguments.Count)
            {
                _diagnostics.ReportWrongArgumentCount(expression.Span, function.Name, function.Parameters.Length, expression.Arguments.Count);
                return new BoundErrorExpression();
            }
            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                ParameterSymbol parameter = function.Parameters[i];
                BoundExpression boundArgument = boundArguments[i];

                if (boundArgument.Type != parameter.Type)
                {
                    _diagnostics.ReportWrongArgumentType(expression.Arguments[i].Span, function.Name, parameter.Name, parameter.Type, boundArgument.Type);
                    return new BoundErrorExpression();
                }
            }
            return new BoundCallExpression(function, boundArguments.ToImmutable());
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            BoundExpression boundExpression = BindExpression(expression.Expression);
            string name = expression.IdentifierToken.Text;
            
            if (!_scope.TryLookup(name, out VariableSymbol? variable))
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Span, name);
                return boundExpression;
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

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol type)
        {
            string name = identifier.Text;
            bool declare = !identifier.IsMissingText;

            VariableSymbol variable = new VariableSymbol(name, type);
            if (declare && !_scope.TryDeclare(variable))
                _diagnostics.ReportVariableAlreadyDeclared(identifier.Span, name);
            return variable;
        }
    }
}