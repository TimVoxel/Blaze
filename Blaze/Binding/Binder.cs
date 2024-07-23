using Blaze.Diagnostics;
using Blaze.IO;
using Blaze.Lowering;
using Blaze.Symbols;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Blaze.Binding
{
    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly FunctionSymbol? _function;

        private Stack<(BoundLabel breakLabel, BoundLabel continueLabel)> _loopStack = new Stack<(BoundLabel breakLabel, BoundLabel continueLabel)>();
        private int _labelCounter = 0;

        private NamespaceSymbol _globalNamespace;
        private NamespaceSymbol _namespace;

        private BoundScope _scope;
        public DiagnosticBag Diagnostics => _diagnostics;

        public Binder(BoundScope? parentScope, FunctionSymbol? function, NamespaceSymbol globalNamespace, NamespaceSymbol thisNamespace)
        {
            _scope = new BoundScope(parentScope);
            _function = function;
            _namespace = thisNamespace;
            _globalNamespace = globalNamespace;

            if (_function != null)
                foreach (var parameter in _function.Parameters)
                    _scope.TryDeclareVariable(parameter);
        }

        public static BoundGlobalScope BindGlobalScope(ImmutableArray<SyntaxTree> syntaxTrees)
        {
            //1. Create a global namespace that contains all the built-in namespaces
            var parentScope = new BoundScope(null);
            var globalNamespace = NamespaceSymbol.CreateGlobal("$global");
            
            foreach (var ns in BuiltInNamespace.GetAll())
                globalNamespace.Members.Add(ns);

            var globalBinder = new Binder(parentScope, null, globalNamespace, globalNamespace);

            //2. Bind all namespace declarations
            var compilationUnits = syntaxTrees.Select(st => st.Root);
            var namespaceDeclarations = compilationUnits.SelectMany(st => st.Namespaces).OrderBy(s => s.IdentifierPath.Count);

            foreach (var ns in namespaceDeclarations)
                globalBinder.BindNamespaceDeclaration(ns);

            //3. Bind usings
            foreach (var compilationUnit in compilationUnits)
                globalBinder.BindUsings(compilationUnit);

            //4. Create the global scope
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(syntaxTrees.SelectMany(d => d.Diagnostics));
            diagnostics.AddRange(globalBinder.Diagnostics);
            
            return new BoundGlobalScope(diagnostics.ToImmutable(), globalNamespace);
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var boundNamespaces = ImmutableDictionary.CreateBuilder<NamespaceSymbol, BoundNamespace>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(globalScope.Diagnostics);

            foreach (var ns in globalScope.GlobalNamespace.AllNestedNamespaces)
            {
                if (!ns.IsBuiltIn)
                {
                    var boundNamespace = BindNamespace(ns, ref diagnostics, globalScope);
                    boundNamespaces.Add(ns, boundNamespace);
                }
            }

            return new BoundProgram(diagnostics.ToImmutable(), boundNamespaces.ToImmutable());
        }

        private static BoundNamespace BindNamespace(NamespaceSymbol ns, ref ImmutableArray<Diagnostic>.Builder diagnostics, BoundGlobalScope globalScope)
        {
            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundStatement>();
            var childrenBuilder = ImmutableDictionary.CreateBuilder<NamespaceSymbol, BoundNamespace>();
            var parentScope = new BoundScope(null);

            foreach (var function in ns.AllFunctions)
            {
                if (function.Declaration != null)
                {
                    var binder = new Binder(parentScope, function, globalScope.GlobalNamespace, ns);

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

            foreach (var child in ns.AllNestedNamespaces)
            {
                var childBoundNamespace = BindNamespace(child, ref diagnostics, globalScope);
                childrenBuilder.Add(child, childBoundNamespace);
            }

            var boundNamespace = new BoundNamespace(ns, childrenBuilder.ToImmutable(), functionBodies.ToImmutable());
            return boundNamespace;
        }

        private void BindUsings(CompilationUnitSyntax compilationUnit)
        {
            //1. Find all symbols declared in the compilation unit
            //2. Search for the namespace in question
            //3. Add the namespace to dictionary for every declared namespace in the compilationUnit

            var namespaces = new List<NamespaceSymbol>();

            foreach (var namespaceDeclaration in compilationUnit.Namespaces)
            {
                if (TryLookupNamespace(namespaceDeclaration.IdentifierPath, out var ns))
                {
                    Debug.Assert(ns != null);
                    namespaces.Add(ns);
                }
                else return;
            }

            var seenUsings = new HashSet<NamespaceSymbol>();

            foreach (var usingSyntax in compilationUnit.Usings)
            {
                if (TryLookupNamespace(usingSyntax.IdentifierPath, out var namespaceToUse))
                {   
                    Debug.Assert(namespaceToUse != null);
                    if (seenUsings.Contains(namespaceToUse))
                    {
                        //TODO: Add warning diagnostics
                        continue;
                    }
                    seenUsings.Add(namespaceToUse);
                    
                    foreach (var ns in namespaces)
                    {
                        if (ns == namespaceToUse)
                            continue;

                        ns.Usings.Add(namespaceToUse);
                    }
                } 
                else
                    _diagnostics.ReportUndefinedNamespace(usingSyntax.Location, usingSyntax.IdentifierPath.Last() .Text);
            }
        }

        private void BindNamespaceDeclaration(NamespaceDeclarationSyntax ns)
        {
            var identifierPath = ns.IdentifierPath;
            var name = identifierPath.First().Text;
            var currentNamespace = _globalNamespace;

            for (int i = 0; i < identifierPath.Count; i++)
            {
                name = identifierPath[i].Text;

                if (name.HasUpperCase())
                    _diagnostics.ReportUpperCaseInNamespaceName(identifierPath[i].Location, name);

                var newNamespace = new NamespaceSymbol(name, currentNamespace, ns);
                if (TryLookupNamespace(name, out var found, currentNamespace))
                {
                    if (i + 1 == identifierPath.Count)
                    {
                        _diagnostics.ReportNamespaceAlreadyDeclared(identifierPath.Last().Location, currentNamespace.GetFullName());
                        break;
                    }
                }
                else
                {
                    currentNamespace.TryDeclareNested(newNamespace);
                    currentNamespace = newNamespace;
                }
            }

            var functionDeclarations = ns.Members.OfType<FunctionDeclarationSyntax>();
            foreach (var function in functionDeclarations)
                BindFunctionDeclaration(function, currentNamespace);

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
                var type = BindType(parameterSyntax.Type.Identifier.Text, parameterSyntax.Type.Location);

                if (type == null)
                    continue;

                if (!seenParameterNames.Add(name))
                    _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, name);
                else
                    parameters.Add(new ParameterSymbol(name, type));
            }

            var returnType = (declaration.ReturnTypeClause == null) ? TypeSymbol.Void : BindType(declaration.ReturnTypeClause.Identifier.Text, declaration.ReturnTypeClause.Identifier.Location);
            if (returnType == null)
                returnType = TypeSymbol.Void;

            var function = new FunctionSymbol(identifierText, ns, parameters.ToImmutable(), returnType, declaration);

            if (!ns.TryDeclareFunction(function))
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
                type = BindType(typeClause.Identifier.Text, typeClause.Identifier.Location);

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

        private BoundExpression BindExpression(ExpressionSyntax expression, bool canBeVoid = false)
        {
            var result = BindExpressionInternal(expression);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(expression.Location);
                return new BoundErrorExpression();
            }
            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax expression)
        {
            return expression.Kind switch
            {
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)expression),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)expression),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)expression),
                SyntaxKind.ParenthesizedExpression => BindExpression(((ParenthesizedExpressionSyntax)expression).Expression),
                SyntaxKind.AssignmentExpression => BindAssignmentExpression((AssignmentExpressionSyntax)expression),
                SyntaxKind.IncrementExpression => BindIncrementExpression((IncrementExpressionSyntax)expression),
                SyntaxKind.ObjectCreationExpression => BindObjectCreationExpression((ObjectCreationExpressionSyntax)expression),

                SyntaxKind.SimpleNameExpression => BindSimpleNameExpression((SimpleNameExpressionSyntax)expression),
                SyntaxKind.CallExpression => BindCallExpression((CallExpressionSyntax)expression),
                SyntaxKind.MemberAccessExpression => BindMemberAccessExpression((MemberAccessExpressionSyntax)expression),
    
                _ => throw new Exception($"Unexpected syntax {expression.Kind}"),
            } ;
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

        private BoundExpression BindSimpleNameExpression(SimpleNameExpressionSyntax expression, NamespaceSymbol? contextNamespace = null)
        {
            var name = expression.IdentifierToken.Text;
            if (expression.IdentifierToken.IsMissingText)
                return new BoundErrorExpression();

            var symbol = LookupSymbol(name, expression.IdentifierToken.Location, contextNamespace);

            if (symbol == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return new BoundErrorExpression();
            }

            if (symbol is VariableSymbol variable)
                return new BoundVariableExpression(variable);

            if (symbol is NamedTypeSymbol type)
                return new BoundTypeExpression(type);

            if (symbol is NamespaceSymbol ns)
                return new BoundNamespaceExpression(ns);

            if (symbol is FunctionSymbol function)
                return new BoundFunctionExpression(function);

            return new BoundErrorExpression();
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax expression, NamespaceSymbol? previous = null)
        {
            if (!expression.AccessedExpression.CanBeAccessed())
            {
                _diagnostics.ReportInvalidMemberAccessLeftKind(expression.AccessedExpression.Location, expression.AccessedExpression.Kind);
                return new BoundErrorExpression();
            }

            var boundAccessed = BindExpression(expression.AccessedExpression);
            var memberName = expression.MemberIdentifier.Text;

            if (boundAccessed.Type.IsError)
                return boundAccessed;

            if (boundAccessed.Type is NamedTypeSymbol namedType)
            {
                //If the expression is of a named type (in other words is an instance of a class)
                //Then check through all the fields. If not found, look through all the methods. If not found,
                //return an error

                Symbol? foundMember = namedType.TryLookup<FieldSymbol>(memberName);
                if (foundMember == null)
                {
                    foundMember = namedType.TryLookup<FunctionSymbol>(memberName);

                    if (foundMember == null)
                    {
                        _diagnostics.ReportUndefinedMemberOfType(expression.MemberIdentifier.Location, namedType.Name, memberName);
                        return new BoundErrorExpression();
                    }
                    else
                        return new BoundMethodAccessExpression(boundAccessed, (FunctionSymbol)foundMember);
                }
                return new BoundFieldAccessExpression(boundAccessed, (FieldSymbol) foundMember);
            }
            else if (boundAccessed is BoundNamespaceExpression namespaceExpression)
            {
                //If the expression is a namespace expression
                //Then check for all nested namespaces and all the functions.
                //Return the according struct (either a BoundNamespaceExpression or a BoundFunctionExpression)

                var foundNested = namespaceExpression.Namespace.TryLookupDirectChild(memberName);
                if (foundNested != null)
                    return new BoundNamespaceExpression(foundNested);

                var foundFunction = namespaceExpression.Namespace.TryLookup<FunctionSymbol>(memberName);
                if (foundFunction != null)
                    return new BoundFunctionExpression(foundFunction);

                _diagnostics.ReportUndefinedMemberOfNamespace(expression.MemberIdentifier.Location, namespaceExpression.Namespace.Name, memberName);
                return new BoundErrorExpression();
            }
            else
            {
                _diagnostics.ReportInvalidMemberAccess(expression.AccessedExpression.Location, boundAccessed.Type.Name);
                return new BoundErrorExpression();
            }
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax expression)
        {
            var boundIdentifier = BindExpression(expression.IdentifierExpression);
            FunctionSymbol function;

            if (boundIdentifier is BoundTypeExpression t)
                return BindConversion(expression.Arguments[0], t.Type, true);            
            else if (boundIdentifier is BoundFunctionExpression functionExpression)
                function = functionExpression.Function;
            else if (boundIdentifier is BoundMethodAccessExpression methodAccessExpression)            
                function = methodAccessExpression.Method;
            else
            {
                _diagnostics.ReportInvalidCallIdentifier(expression.IdentifierExpression.Location, boundIdentifier.Kind);
                return new BoundErrorExpression();
            }    

            if (function.Parameters.Length != expression.Arguments.Count)
            {
                _diagnostics.ReportWrongArgumentCount(expression.Location, function.Name, function.Parameters.Length, expression.Arguments.Count);
                return new BoundErrorExpression();
            }

            var arguments = BindArguments(expression.Arguments, function.Parameters);
            return new BoundCallExpression(boundIdentifier, function, arguments);
        }
        
        private BoundExpression BindObjectCreationExpression(ObjectCreationExpressionSyntax syntax)
        {
            var name = syntax.Identifier.Text;
            var classSymbol = _namespace.TryLookup<NamedTypeSymbol>(name);

            if (classSymbol == null)
            {
                _diagnostics.ReportUndefinedClass(syntax.Identifier.Location, name);
                return new BoundErrorExpression();
            }

            if (classSymbol.Constructor.Parameters.Length != syntax.Arguments.Count)
            {
                _diagnostics.ReportWrongConstructorArgumentCount(syntax.Location, classSymbol.Name, classSymbol.Constructor.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            var arguments = BindArguments(syntax.Arguments, classSymbol.Constructor.Parameters);
            return new BoundObjectCreationExpression(classSymbol, arguments);
        }

        private ImmutableArray<BoundExpression> BindArguments(SeparatedSyntaxList<ExpressionSyntax> arguments, ImmutableArray<ParameterSymbol> parameters)
        {
            var boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (var argument in arguments)
                boundArguments.Add(BindExpression(argument));

            for (int i = 0; i < arguments.Count; i++)
            {
                var argumentLocation = arguments[i].Location;
                var parameter = parameters[i];
                var boundArgument = boundArguments[i];
                boundArguments[i] = BindConversion(boundArgument, parameter.Type, argumentLocation);
            }

            return boundArguments.ToImmutable();
        } 

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            var boundLeft = BindExpression(expression.Left);
            var boundRight = BindExpression(expression.Right);
            
            if (boundLeft.Kind != BoundNodeKind.VariableExpression &&
                boundLeft.Kind != BoundNodeKind.FieldAccessExpression)
            {
                _diagnostics.ReportInvalidLeftHandAssignmentExpression(expression.Left.Location, expression.Left.Kind);
                return new BoundErrorExpression();
            }

            if (expression.AssignmentToken.Kind != SyntaxKind.EqualsToken)
            {
                var correspondingBinaryOperatorKind = SyntaxFacts.GetCorrespondingBinaryOperatorKind(expression.AssignmentToken.Kind);
                var boundOperator = BoundBinaryOperator.Bind(correspondingBinaryOperatorKind, boundLeft.Type, boundRight.Type);

                if (boundOperator == null)
                {
                    _diagnostics.ReportUndefinedBinaryOperator(expression.AssignmentToken.Location, expression.AssignmentToken.Text, boundLeft.Type, boundRight.Type);
                    return new BoundErrorExpression();
                }

                var convertedExpression = BindConversion(boundRight, boundLeft.Type, expression.Right.Location);
                return new BoundCompoundAssignmentExpression(boundLeft, boundOperator, convertedExpression);
            }
            else
            {
                var convertedExpression = BindConversion(boundRight, boundLeft.Type, expression.Right.Location);
                return new BoundAssignmentExpression(boundLeft, convertedExpression);
            }
        }

        private BoundExpression BindIncrementExpression(IncrementExpressionSyntax expression)
        {
            var boundLeft = BindExpression(expression.Expression);
            
            if (boundLeft.Kind != BoundNodeKind.VariableExpression &&
                boundLeft.Kind != BoundNodeKind.FieldAccessExpression)
            {
                _diagnostics.ReportInvalidIncrementExpression(expression.Expression.Location, expression.Expression.Kind);
                return new BoundErrorExpression();
            }

            var correspondingBinaryOperatorKind = SyntaxFacts.GetCorrespondingBinaryOperatorKind(expression.AssignmentToken.Kind);
            var boundOperator = BoundBinaryOperator.Bind(correspondingBinaryOperatorKind, boundLeft.Type, TypeSymbol.Int);

            if (boundOperator == null)
            {
                _diagnostics.ReportUndefinedIncrementOperator(expression.AssignmentToken.Location, expression.AssignmentToken.Text, boundLeft.Type);
                return new BoundErrorExpression();
            }

            return new BoundIncrementExpression(boundLeft, boundOperator);
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

        private TypeSymbol? BindType(string name, TextLocation location)
        {
            var type = LookupType(name);

            if (type == null)
                _diagnostics.ReportUndefinedType(location, name);

            return type;
        }

        private TypeSymbol? LookupType(string name)
        {
            TypeSymbol? type = TypeSymbol.Lookup(name);
            if (type == null)
                type = _namespace.TryLookup<NamedTypeSymbol>(name);

            return type;
        }

        private Symbol? LookupSymbol(string name, TextLocation identifierLocation, NamespaceSymbol? contextNamespace = null)
        {
            //1. Try lookup a local variable in the current scope
            //2. If not found, try to find a type with the name {name}
            //3. If not found, try to find a namespace with the name {name}
            //4. If not found, try to find a function with the name {name}

            var variable = _scope.TryLookupVariable(name);
            if (variable != null)
                return variable;
            
            var lookupDomain = contextNamespace ?? _namespace;
            var nestedNamespace = lookupDomain.TryLookupDirectChild(name);

            if (nestedNamespace != null)
                return nestedNamespace;

            var namedType = lookupDomain.TryLookup<NamedTypeSymbol>(name);
            if (namedType != null)
                return namedType;

            return lookupDomain.TryLookup<FunctionSymbol>(name);
        }

        private bool TryLookupNamespace(string name, out NamespaceSymbol? ns, NamespaceSymbol? previous = null)
        {
            if (previous == null)
                ns = _globalNamespace.TryLookupDirectChild(name);
            else
                ns = previous.TryLookupDirectChild(name);

            return ns != null;
        }

        private bool TryLookupNamespace(SeparatedSyntaxList<SyntaxToken> qualifiedName, out NamespaceSymbol? ns)
        {
            ns = null;

            for (int i = 0; i < qualifiedName.Count; i++)
            {
                var previous = ns;
                var name = qualifiedName[i].Text;
                if (!TryLookupNamespace(name, out ns, previous))
                    return false;
            }
            return true;
        }
    }
}