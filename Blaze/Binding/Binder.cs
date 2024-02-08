using Blaze.Diagnostics;
using Blaze.Lowering;
using Blaze.Symbols;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly FunctionSymbol? _function;

        private Stack<(BoundLabel breakLabel, BoundLabel continueLabel)> _loopStack = new Stack<(BoundLabel breakLabel, BoundLabel continueLabel)>();
        private int _labelCounter = 0;

        private BoundScope _scope;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Binder(BoundScope? parent, FunctionSymbol? function)
        {
            _scope = new BoundScope(parent);
            _function = function;

            if (_function != null)
                foreach (var parameter in _function.Parameters)
                    _scope.TryDeclareVariable(parameter);
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
        {
            BoundScope? parentScope = CreateParentScope(previous);
            Binder binder = new Binder(parentScope, null);

            foreach (FunctionDeclarationSyntax function in syntax.Members.OfType<FunctionDeclarationSyntax>())
                binder.BindFunctionDeclaration(function);

            //Bind global statements
            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();
            foreach (var globalStatement in syntax.Members.OfType<GlobalStatementSyntax>())
            {
                BoundStatement statement = binder.BindStatement(globalStatement.Statement);
                statements.Add(statement);
            }

            ImmutableArray<FunctionSymbol> functions = binder._scope.GetDeclaredFunctions();
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();
            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();

            if (previous != null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, variables, functions, statements.ToImmutable());
        }

        private static BoundScope? CreateParentScope(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope? parent = CreateRootScope();
            
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new BoundScope(parent);

                foreach (FunctionSymbol function in previous.Functions)
                    scope.TryDeclareFunction(function);

                foreach (VariableSymbol variable in previous.Variables)
                    scope.TryDeclareVariable(variable);

                parent = scope;
            }
            return parent;
        }

        private static BoundScope CreateRootScope()
        {
            BoundScope result = new BoundScope(null);
            foreach (FunctionSymbol? builtInFunction in BuiltInFunction.GetAll())
                result.TryDeclareFunction(builtInFunction);
            return result;
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            BoundScope? parentScope = CreateParentScope(globalScope);
            ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Builder functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
           
            ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            BoundGlobalScope? scope = globalScope;
            while (scope != null)
            {
                foreach (FunctionSymbol function in scope.Functions)
                {
                    Binder binder = new Binder(parentScope, function);
                    if (function.Declaration != null)
                    {
                        BoundStatement body = binder.BindStatement(function.Declaration.Body);
                        BoundBlockStatement loweredBody = Lowerer.Lower(body);

                        if (function.ReturnType != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                            binder._diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);

                        functionBodies.Add(function, loweredBody);
                        diagnostics.AddRange(binder.Diagnostics);
                    }
                }
                scope = scope.Previous;
            }

            BoundBlockStatement statement = Lowerer.Lower(new BoundBlockStatement(globalScope.Statements));
            return new BoundProgram(diagnostics.ToImmutable(), functionBodies.ToImmutable(), statement);
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax declaration)
        {
            ImmutableArray<ParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            var seenParameterNames = new HashSet<string>();
            foreach (ParameterSyntax parameterSyntax in declaration.Parameters)
            {
                string name = parameterSyntax.Identifier.Text;
                TypeSymbol? type = BindTypeClause(parameterSyntax.Type);
                if (type == null)
                    continue;

                if (!seenParameterNames.Add(name))
                    _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, name);
                else
                    parameters.Add(new ParameterSymbol(name, type));
            }

            TypeSymbol? returnType = (declaration.ReturnTypeClause == null) ? TypeSymbol.Void : BindTypeClause(declaration.ReturnTypeClause);
            if (returnType == null)
                returnType = TypeSymbol.Void;

            FunctionSymbol function = new FunctionSymbol(declaration.Identifier.Text, parameters.ToImmutable(), returnType, declaration);
            if (!_scope.TryDeclareFunction(function))
                _diagnostics.ReportFunctionAlreadyDeclared(declaration.Identifier.Location, function.Name);
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
                case SyntaxKind.DoWhileStatement:
                    return BindDoWhileStatement((DoWhileStatementSyntax)syntax);
                case SyntaxKind.BreakStatement:
                    return BindBreakStatement((BreakStatementSyntax)syntax);
                case SyntaxKind.ContinueStatement:
                    return BindContinueStatement((ContinueStatementSyntax)syntax);
                case SyntaxKind.ReturnStatement:
                    return BindReturnStatement((ReturnStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            BoundExpression initializer = BindExpression(syntax.Initializer);
            TypeSymbol? type = null;
            if (syntax.DeclarationNode is TypeClauseSyntax typeClause)
            {
                type = BindTypeClause(typeClause);
                if (type == null)
                    type = initializer.Type;
                else 
                    initializer = BindConversion(initializer, type, syntax.Initializer.Location);
            }
            else
                type = initializer.Type;

            VariableSymbol variable = BindVariable(syntax.Identifier, type);
            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            BoundExpression boundExpression = BindExpression(syntax.Expression, true);
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
            BoundStatement body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            return new BoundWhileStatement(boundCondition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindDoWhileStatement(DoWhileStatementSyntax syntax)
        {
            BoundStatement body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            BoundExpression condition = BindExpression(syntax.Condition);
            return new BoundDoWhileStatement(body, condition, breakLabel, continueLabel);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            BoundExpression lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int);
            BoundExpression upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int);

            BoundScope previous = _scope;
            _scope = new BoundScope(previous);

            VariableSymbol variable = BindVariable(syntax.Identifier, TypeSymbol.Int);
            BoundStatement body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);

            _scope = previous;

            return new BoundForStatement(variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            BoundExpression? expression = (syntax.Expression == null) ? null : BindExpression(syntax.Expression);

            if (_function == null)
            {
                _diagnostics.ReportReturnOutsideFunction(syntax.Keyword.Location);
            }
            else
            {
                if (_function.ReturnType == TypeSymbol.Void)
                {
                    if (syntax.Expression != null)
                        _diagnostics.ReportInvalidReturnExpression(syntax.Expression.Location, _function.Name);
                }
                else
                {
                    if (syntax.Expression == null || expression == null)
                        _diagnostics.ReportMissingReturnExpression(syntax.Keyword.Location, _function.Name, _function.ReturnType);
                    else
                        expression = BindConversion(expression, _function.ReturnType, syntax.Expression.Location);
                }
            }
            return new BoundReturnStatement(expression);
        }

        private BoundStatement BindLoopBody(StatementSyntax body, out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            _labelCounter++;
            breakLabel = new BoundLabel($"break{_labelCounter}");
            continueLabel = new BoundLabel($"continue{_labelCounter}");

            _loopStack.Push((breakLabel, continueLabel));
            BoundStatement boundBody = BindStatement(body);
            _loopStack.Pop();
            return boundBody;
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }
            BoundLabel breakLabel = _loopStack.Peek().breakLabel;
            return new BoundGotoStatement(breakLabel);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }
            BoundLabel continueLabel = _loopStack.Peek().continueLabel;
            return new BoundGotoStatement(continueLabel);
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

        private BoundStatement BindErrorStatement() => new BoundExpressionStatement(new BoundErrorExpression());

        private BoundExpression BindExpression(ExpressionSyntax expression, bool canBeVoid = false)
        {
            BoundExpression result = BindExpressionInternal(expression);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(expression.Location);
                return new BoundErrorExpression();
            }
            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax expression)
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

        private BoundExpression BindExpression(ExpressionSyntax expression, TypeSymbol desiredType) => BindConversion(expression, desiredType);

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
                _diagnostics.ReportUndefinedBinaryOperator(expression.OperatorToken.Location, expression.OperatorToken.Text, boundLeft.Type, boundRight.Type);
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
                _diagnostics.ReportUndefinedUnaryOperator(expression.OperatorToken.Location, expression.OperatorToken.Text, operand.Type);
                return new BoundErrorExpression();
            }
            return new BoundUnaryExpression(op, operand);
        }

        private BoundExpression BindIdentifierExpression(IdentifierExpressionSyntax expression)
        {
            string name = expression.IdentifierToken.Text;
            if (expression.IdentifierToken.IsMissingText)
                return new BoundErrorExpression();

            VariableSymbol? variable = _scope.TryLookupVariable(name);
            if (variable == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return new BoundErrorExpression();
            }
            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax expression)
        {
            string name = expression.Identifier.Text;
            if (expression.Arguments.Count == 1 && LookupType(name) is TypeSymbol type)
                return BindConversion(expression.Arguments[0], type, true);
            
            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (ExpressionSyntax argument in expression.Arguments)
                boundArguments.Add(BindExpression(argument));

            FunctionSymbol? function = _scope.TryLookupFunction(expression.Identifier.Text);
            if (function == null)
            {
                _diagnostics.ReportUndefinedFunction(expression.Identifier.Location, name);
                return new BoundErrorExpression();
            }
            if (function.Parameters.Length != expression.Arguments.Count)
            {
                _diagnostics.ReportWrongArgumentCount(expression.Location, function.Name, function.Parameters.Length, expression.Arguments.Count);
                return new BoundErrorExpression();
            }
            bool hasErrors = false;
            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                ParameterSymbol parameter = function.Parameters[i];
                BoundExpression boundArgument = boundArguments[i];

                if (boundArgument.Type != parameter.Type)
                {
                    if (boundArgument.Type != TypeSymbol.Error)
                        _diagnostics.ReportWrongArgumentType(expression.Arguments[i].Location, function.Name, parameter.Name, parameter.Type, boundArgument.Type);
                    hasErrors = true;
                }
            }
            if (hasErrors)
                return new BoundErrorExpression();

            return new BoundCallExpression(function, boundArguments.ToImmutable());
        }
        
        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            BoundExpression boundExpression = BindExpression(expression.Expression);
            string name = expression.IdentifierToken.Text;

            VariableSymbol? variable = _scope.TryLookupVariable(name);
            if (variable == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return boundExpression;
            }

            BoundExpression convertedExpression = BindConversion(boundExpression, variable.Type, expression.Expression.Location);
            return new BoundAssignmentExpression(variable, convertedExpression);
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            BoundExpression expression = BindExpression(syntax);
            return BindConversion(expression, type, syntax.Location, allowExplicit);
        }

        private BoundExpression BindConversion(BoundExpression expression, TypeSymbol type, TextLocation diagnosticLocation, bool allowExplicit = false)
        {
            Conversion conversion = Conversion.Classify(expression.Type, type);
            if (!conversion.Exists)
            {
                if (!expression.Type.IsError && !type.IsError)
                    _diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);

                return new BoundErrorExpression();
            }

            if (conversion == Conversion.Identity)
                return expression;

            if (conversion.IsExplicit && !allowExplicit)
                _diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);
                
            return new BoundConversionExpression(type, expression);
        }

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol type)
        {
            string name = identifier.Text;
            bool declare = !identifier.IsMissingText;

            VariableSymbol variable = (_function == null) ? 
                new GlobalVariableSymbol(name, type) 
              : new LocalVariableSymbol(name, type);
            if (declare && !_scope.TryDeclareVariable(variable))
                _diagnostics.ReportVariableAlreadyDeclared(identifier.Location, name);
            return variable;
        }

        private TypeSymbol? BindTypeClause(TypeClauseSyntax syntax)
        {
            TypeSymbol? type = LookupType(syntax.Identifier.Text);
            if (type == null)
                _diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);
            return type;
        }

        private TypeSymbol? BindTypeClause(ReturnTypeClauseSyntax syntax)
        {
            TypeSymbol? type = LookupType(syntax.Identifier.Text);
            if (type == null)
                _diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);
            return type;
        }

        private TypeSymbol? LookupType(string name)
        {
            switch (name)
            {
                case "bool":
                    return TypeSymbol.Bool;
                case "string":
                    return TypeSymbol.String;
                case "int":
                    return TypeSymbol.Int;
                default:
                    return null;
            }
        }
    }
}