using Blaze.Diagnostics;
using Blaze.Lowering;
using Blaze.Symbols;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Blaze.Binding
{
    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly FunctionSymbol? _function;

        private Stack<(BoundLabel breakLabel, BoundLabel continueLabel)> _loopStack = new Stack<(BoundLabel breakLabel, BoundLabel continueLabel)>();
        private int _labelCounter = 0;

        private List<NamespaceSymbol> _namespaces = new List<NamespaceSymbol>();
        private BoundScope _scope;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Binder(BoundScope? parent, FunctionSymbol? function, ImmutableArray<NamespaceSymbol>? definedNamespaces)
        {
            _scope = new BoundScope(parent);
            _function = function;

            if (definedNamespaces != null)
                _namespaces.AddRange(definedNamespaces);

            if (_function != null)
                foreach (var parameter in _function.Parameters)
                    _scope.TryDeclareVariable(parameter);
        }

        public static BoundGlobalScope BindGlobalScope(ImmutableArray<SyntaxTree> syntaxTrees)
        {
            //1. Create a root scope that contains all the built-in functions 
            var parentScope = CreateRootScope();
            var binder = new Binder(parentScope, null, null);
            
            //2. Bind all namespace declarations
            var namespaceDeclarations = syntaxTrees.SelectMany(st => st.Root.Namespaces)
                .OrderBy(s => s.IdentifierPath.Count);

            foreach (var ns in namespaceDeclarations)
                binder.BindNamespaceDeclaration(ns);

            //3. Create the global scope
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(syntaxTrees.SelectMany(d => d.Diagnostics));
            diagnostics.AddRange(binder.Diagnostics);
            
            var namespaces = binder._namespaces.ToImmutableArray();

            return new BoundGlobalScope(diagnostics.ToImmutable(), namespaces);
        }

        private static BoundScope CreateRootScope()
        {
            var result = new BoundScope(null);

            foreach (var builtInFunction in BuiltInFunction.GetAll())
                result.TryDeclareFunction(builtInFunction);

            return result;
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var boundNamespaces = ImmutableDictionary.CreateBuilder<NamespaceSymbol, BoundNamespace>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(globalScope.Diagnostics);

            //2. Bind every namespace

            foreach (var ns in globalScope.Namespaces)
            {
                var boundNamespace = BindNamespace(ns, ref diagnostics, globalScope.Namespaces);
                boundNamespaces.Add(ns, boundNamespace);
            }

            return new BoundProgram(diagnostics.ToImmutable(), boundNamespaces.ToImmutable());
        }

        private static BoundNamespace BindNamespace(NamespaceSymbol ns, ref ImmutableArray<Diagnostic>.Builder diagnostics, ImmutableArray<NamespaceSymbol> definedNamespaces)
        {
            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundStatement>();
            var childrenBuilder = ImmutableDictionary.CreateBuilder<NamespaceSymbol, BoundNamespace>();

            foreach (var function in ns.Scope.GetDeclaredFunctions())
            {
                if (function.Declaration != null)
                {
                    var binder = new Binder(ns.Scope, function, definedNamespaces);

                    var body = binder.BindStatement(function.Declaration.Body);
                    var loweredBody = Lowerer.Lower(body);
                    var deepLoweredBody = Lowerer.DeepLower(body);

                    if (function.ReturnType != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(deepLoweredBody))
                        binder._diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);

                    //Passing the lowered body, not the deep lowered one, as it is
                    //Easier to convert to mcfunctions
                    functionBodies.Add(function, loweredBody);
                    diagnostics.AddRange(binder.Diagnostics);
                }
            }

            foreach (var child in ns.Children)
            {
                var childBoundNamespace = BindNamespace(child, ref diagnostics, definedNamespaces);
                childrenBuilder.Add(child, childBoundNamespace);
            }

            var boundNamespace = new BoundNamespace(ns, childrenBuilder.ToImmutable(), functionBodies.ToImmutable());
            return boundNamespace;
        }

        private void BindNamespaceDeclaration(NamespaceDeclarationSyntax ns)
        {
            var identifierPath = ns.IdentifierPath;
            var name = identifierPath.First().Text;
            NamespaceSymbol? currentNamespace = null;

            var previousScope = _scope;

            for (int i = 0; i < identifierPath.Count; i++)
            {
                var previous = currentNamespace;
                name = identifierPath[i].Text;

                if (name.ToLower() != name)
                    _diagnostics.ReportUpperCaseInNamespaceName(identifierPath[i].Location, name);

                if (TryLookupNamespace(name, out currentNamespace, previous))
                {
                    Debug.Assert(currentNamespace != null);

                    if (identifierPath.Count == i + 1)
                    {
                        _diagnostics.ReportNamespaceAlreadyDeclared(identifierPath.Last().Location, currentNamespace.GetFullName());
                        break;
                    }
                    else
                        _scope = currentNamespace.Scope;
                }
                else
                {
                    _scope = new BoundScope(_scope);
                    var newNamespace = new NamespaceSymbol(name, _scope, previous, ns);
                    if (previous != null)
                        previous.Children.Add(newNamespace);
                    else
                        _namespaces.Add(newNamespace);

                    currentNamespace = newNamespace;
                }
            }

            Debug.Assert(currentNamespace != null);

            var functionDeclarations = ns.Members.OfType<FunctionDeclarationSyntax>();

            foreach (var function in functionDeclarations)
                BindFunctionDeclaration(function, currentNamespace);

            _scope = previousScope;
            /*
            var globalStatements = ns.Members.OfType<GlobalStatementSyntax>();
            var statements = ImmutableArray.CreateBuilder<BoundStatement>();

            foreach (var globalStatement in globalStatements)
            {
                var boundStatement = BindGlobalStatement(globalStatement.Statement);
                statements.Add(boundStatement);
            }
            */
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax declaration, NamespaceSymbol ns)
        {
            var identifierText = declaration.Identifier.Text;

            if (identifierText.ToLower() != identifierText)
                _diagnostics.ReportUpperCaseInFunctionName(declaration.Identifier.Location, identifierText);

            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
            var seenParameterNames = new HashSet<string>();

            foreach (var parameterSyntax in declaration.Parameters)
            {
                var name = parameterSyntax.Identifier.Text;
                var type = BindTypeClause(parameterSyntax.Type);

                if (type == null)
                    continue;

                if (!seenParameterNames.Add(name))
                    _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, name);
                else
                    parameters.Add(new ParameterSymbol(name, type));
            }

            var returnType = (declaration.ReturnTypeClause == null) ? TypeSymbol.Void : BindReturnTypeClause(declaration.ReturnTypeClause);
            if (returnType == null)
                returnType = TypeSymbol.Void;

            var function = new FunctionSymbol(identifierText, ns, parameters.ToImmutable(), returnType, declaration);

            if (!ns.Scope.TryDeclareFunction(function))
                _diagnostics.ReportFunctionAlreadyDeclared(declaration.Identifier.Location, function.Name);
        }

        private BoundStatement BindGlobalStatement(StatementSyntax syntax) => BindStatement(syntax, true);

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            var result = BindStatementInternal(syntax);

            if (isGlobal)
                return result;

            if (result is BoundExpressionStatement es)
            {
                var isAllowedExpression = es.Expression.Kind == BoundNodeKind.AssignmentExpression
                                        || es.Expression.Kind == BoundNodeKind.CompoundAssignmentExpression
                                        || es.Expression.Kind == BoundNodeKind.CallExpression
                                        || es.Expression.Kind == BoundNodeKind.IncrementExpression
                                        || es.Expression.Kind == BoundNodeKind.ErrorExpression;

                if (!isAllowedExpression)
                    _diagnostics.ReportInvalidExpressionStatement(syntax.Location);
            }
            return result;
        }

        private BoundStatement BindStatementInternal(StatementSyntax syntax)
        {
            return syntax.Kind switch
            {
                SyntaxKind.BlockStatement => BindBlockStatement((BlockStatementSyntax)syntax),
                SyntaxKind.ExpressionStatement => BindExpressionStatement((ExpressionStatementSyntax)syntax),
                SyntaxKind.VariableDeclarationStatement => BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax),
                SyntaxKind.IfStatement => BindIfStatement((IfStatementSyntax)syntax),
                SyntaxKind.WhileStatement => BindWhileStatement((WhileStatementSyntax)syntax),
                SyntaxKind.ForStatement => BindForStatement((ForStatementSyntax)syntax),
                SyntaxKind.DoWhileStatement => BindDoWhileStatement((DoWhileStatementSyntax)syntax),
                SyntaxKind.BreakStatement => BindBreakStatement((BreakStatementSyntax)syntax),
                SyntaxKind.ContinueStatement => BindContinueStatement((ContinueStatementSyntax)syntax),
                SyntaxKind.ReturnStatement => BindReturnStatement((ReturnStatementSyntax)syntax),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            var initializer = BindExpression(syntax.Initializer);
            TypeSymbol? type = null;
            if (syntax.DeclarationNode is TypeClauseSyntax typeClause)
                type = BindTypeClause(typeClause);

            var variableType = type ?? initializer.Type;
            var variable = BindVariable(syntax.Identifier, variableType, initializer.ConstantValue);
            var convertedInitializer = BindConversion(initializer, variableType, syntax.Initializer.Location);
            return new BoundVariableDeclarationStatement(variable, convertedInitializer);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var boundExpression = BindExpression(syntax.Expression, true);
            return new BoundExpressionStatement(boundExpression);
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var boundCondition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindStatement(syntax.Body);
            var elseBody = (syntax.ElseClause == null) ? null : BindStatement(syntax.ElseClause.Body);
            return new BoundIfStatement(boundCondition, body, elseBody);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var boundCondition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            return new BoundWhileStatement(boundCondition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindDoWhileStatement(DoWhileStatementSyntax syntax)
        {
            var body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            var condition = BindExpression(syntax.Condition);
            return new BoundDoWhileStatement(body, condition, breakLabel, continueLabel);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            var lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int);
            var upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int);
            var previous = _scope;
            _scope = new BoundScope(previous);

            var variable = BindVariable(syntax.Identifier, TypeSymbol.Int);
            var body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            _scope = previous;
            return new BoundForStatement(variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            var expression = (syntax.Expression == null) ? null : BindExpression(syntax.Expression);

            if (_function == null)
                _diagnostics.ReportReturnOutsideFunction(syntax.Location);
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
            var boundBody = BindStatement(body);
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
            var breakLabel = _loopStack.Peek().breakLabel;
            return new BoundBreakStatement(breakLabel);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }
            var continueLabel = _loopStack.Peek().continueLabel;
            return new BoundContinueStatement(continueLabel);
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            var boundStatements = ImmutableArray.CreateBuilder<BoundStatement>();
            var previous = _scope;
            _scope = new BoundScope(previous);

            foreach (var statement in syntax.Statements)
            {
                var boundStatement = BindStatement(statement);
                boundStatements.Add(boundStatement);
            }

            _scope = previous;
            return new BoundBlockStatement(boundStatements.ToImmutable());
        }

        private BoundStatement BindErrorStatement() => new BoundExpressionStatement(new BoundErrorExpression());

        private BoundExpression BindExpression(ExpressionSyntax expression, bool canBeVoid = false, NamespaceSymbol? namespaceSymbol = null)
        {
            var result = BindExpressionInternal(expression, namespaceSymbol);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(expression.Location);
                return new BoundErrorExpression();
            }
            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax expression, NamespaceSymbol? namespaceSymbol)
        {
            return expression.Kind switch
            {
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)expression),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)expression),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)expression),
                SyntaxKind.ParenthesizedExpression => BindExpression(((ParenthesizedExpressionSyntax)expression).Expression),
                SyntaxKind.IdentifierExpression => BindIdentifierExpression((IdentifierExpressionSyntax)expression),
                SyntaxKind.AssignmentExpression => BindAssignmentExpression((AssignmentExpressionSyntax)expression),
                SyntaxKind.IncrementExpression => BindIncrementExpression((IncrementExpressionSyntax)expression),

                SyntaxKind.CallExpression => BindCallExpression((CallExpressionSyntax)expression, namespaceSymbol),
                SyntaxKind.MemberAccessExpression => BindMemberAccessExpression((MemberAccessExpressionSyntax)expression, namespaceSymbol),
                _ => throw new Exception($"Unexpected syntax {expression.Kind}"),
            };
        }

        private BoundExpression BindExpression(ExpressionSyntax expression, TypeSymbol desiredType) => BindConversion(expression, desiredType);

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax expression)
        {
            var value = expression.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax expression)
        {
            var boundLeft = BindExpression(expression.Left);
            var boundRight = BindExpression(expression.Right);

            if (boundLeft.Type.IsError || boundRight.Type.IsError)
                return new BoundErrorExpression();

            var op = BoundBinaryOperator.Bind(expression.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (op == null)
            {
                _diagnostics.ReportUndefinedBinaryOperator(expression.OperatorToken.Location, expression.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression();
            }
            return new BoundBinaryExpression(boundLeft, op, boundRight);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax expression)
        {
            var operand = BindExpression(expression.Operand);
            if (operand.Type.IsError)
                return new BoundErrorExpression();

            var op = BoundUnaryOperator.Bind(expression.OperatorToken.Kind, operand.Type);

            if (op == null)
            {
                _diagnostics.ReportUndefinedUnaryOperator(expression.OperatorToken.Location, expression.OperatorToken.Text, operand.Type);
                return new BoundErrorExpression();
            }
            return new BoundUnaryExpression(op, operand);
        }

        private BoundExpression BindIdentifierExpression(IdentifierExpressionSyntax expression)
        {
            var name = expression.IdentifierToken.Text;
            if (expression.IdentifierToken.IsMissingText)
                return new BoundErrorExpression();

            var variable = _scope.TryLookupVariable(name);
            if (variable == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return new BoundErrorExpression();
            }
            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax expression, NamespaceSymbol? previous = null)
        {
            var name = expression.Identifier.Text;
            if (expression.Identifier.IsMissingText)
                return new BoundErrorExpression();

            if (expression.Member.Kind != SyntaxKind.CallExpression && expression.Member.Kind != SyntaxKind.MemberAccessExpression)
            {
                _diagnostics.ReportInvalidMemberAccessExpressionKind(expression.Member.Location);
                return new BoundErrorExpression();
            }

            if (TryLookupNamespace(name, out var parentSymbol, previous))
            {
                Debug.Assert(parentSymbol != null);
                var boundExpression = BindExpression(expression.Member, true, parentSymbol);
                return boundExpression;  
            }
            else
            {
                _diagnostics.ReportUndefinedNamespace(expression.Identifier.Location, name);
                return new BoundErrorExpression();
            }
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax expression, NamespaceSymbol? parentSymbol = null)
        {
            var scope = parentSymbol == null ? _scope : parentSymbol.Scope;
            var name = expression.Identifier.Text;

            if (expression.Arguments.Count == 1 && TypeSymbol.Lookup(name) is TypeSymbol type)
                return BindConversion(expression.Arguments[0], type, true);
            
            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (ExpressionSyntax argument in expression.Arguments)
                boundArguments.Add(BindExpression(argument));

            var function = scope.TryLookupFunction(expression.Identifier.Text);
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

            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                var argumentLocation = expression.Arguments[i].Location;
                var parameter = function.Parameters[i];
                var boundArgument = boundArguments[i];
                boundArguments[i] = BindConversion(boundArgument, parameter.Type, argumentLocation);
            }
            
            return new BoundCallExpression(function, boundArguments.ToImmutable());
        }
        
        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            var boundExpression = BindExpression(expression.Expression);
            var name = expression.IdentifierToken.Text;

            var variable = _scope.TryLookupVariable(name);
            if (variable == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return new BoundErrorExpression();
            }

            if (expression.AssignmentToken.Kind != SyntaxKind.EqualsToken)
            {
                var correspondingBinaryOperatorKind = SyntaxFacts.GetCorrespondingBinaryOperatorKind(expression.AssignmentToken.Kind);
                var boundOperator = BoundBinaryOperator.Bind(correspondingBinaryOperatorKind, variable.Type, boundExpression.Type);

                if (boundOperator == null)
                {
                    _diagnostics.ReportUndefinedBinaryOperator(expression.AssignmentToken.Location, expression.AssignmentToken.Text, variable.Type, boundExpression.Type);
                    return new BoundErrorExpression();
                }

                var convertedExpression = BindConversion(boundExpression, variable.Type, expression.Expression.Location);
                return new BoundCompoundAssignmentExpression(variable, boundOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(boundExpression, variable.Type, expression.Expression.Location);
                return new BoundAssignmentExpression(variable, convertedExpression);
            }
        }

        private BoundExpression BindIncrementExpression(IncrementExpressionSyntax expression)
        {
            var name = expression.IdentifierToken.Text;

            var variable = _scope.TryLookupVariable(name);
            if (variable == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return new BoundErrorExpression();
            }

            var correspondingBinaryOperatorKind = SyntaxFacts.GetCorrespondingBinaryOperatorKind(expression.AssignmentToken.Kind);
            var boundOperator = BoundBinaryOperator.Bind(correspondingBinaryOperatorKind, variable.Type, TypeSymbol.Int);

            if (boundOperator == null)
            {
                _diagnostics.ReportUndefinedIncrementOperator(expression.AssignmentToken.Location, expression.AssignmentToken.Text, variable.Type);
                return new BoundErrorExpression();
            }

            return new BoundIncrementExpression(variable, boundOperator);
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            var expression = BindExpression(syntax);
            return BindConversion(expression, type, syntax.Location, allowExplicit);
        }

        private BoundExpression BindConversion(BoundExpression expression, TypeSymbol type, TextLocation diagnosticLocation, bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);
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

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol type, BoundConstant? constant = null)
        {
            var name = identifier.Text;
            VariableSymbol variable = _function == null
                                ? new GlobalVariableSymbol(name, type, constant)
                                : new LocalVariableSymbol(name, type, constant);

            if (!_scope.TryDeclareVariable(variable))
                _diagnostics.ReportVariableAlreadyDeclared(identifier.Location, name);

            return variable;
        }

        private TypeSymbol? BindTypeClause(TypeClauseSyntax syntax)
        {
            var type = TypeSymbol.Lookup(syntax.Identifier.Text);
            if (type == null)
                _diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);
            return type;
        }

        private TypeSymbol? BindReturnTypeClause(ReturnTypeClauseSyntax syntax)
        {
            var type = TypeSymbol.Lookup(syntax.Identifier.Text);
            if (type == null)
                _diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);
            return type;
        }

        private bool TryLookupNamespace(string name, out NamespaceSymbol? ns, NamespaceSymbol? previous = null)
        {
            Console.WriteLine("Searching for " + name + " in " + previous);
            foreach (var nsa in _namespaces)
                Console.WriteLine(nsa);

            if (previous == null)
                ns = _namespaces.FirstOrDefault(n => n.Name == name);
            else
                ns = previous.TryLookupChild(name);

            return ns != null;
        }
    }
}