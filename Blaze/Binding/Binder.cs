using Blaze.Binding.Lookup;
using Blaze.Diagnostics;
using Blaze.IO;
using Blaze.Lowering;
using Blaze.Symbols;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Blaze.Binding
{
    //TODO: Add better lookup to avoid duplicating name errors

    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly FunctionSymbol? _function;
        private readonly NamespaceSymbol _globalNamespace;
        private readonly NamespaceSymbol _namespace;

        private Stack<(BoundLabel breakLabel, BoundLabel continueLabel)> _loopStack = new Stack<(BoundLabel breakLabel, BoundLabel continueLabel)>();
        private int _labelCounter = 0;

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
                globalNamespace.TryDeclareNested(ns);

            var globalBinder = new Binder(parentScope, null, globalNamespace, globalNamespace);

            //2. Bind all namespace declarations
            var compilationUnits = syntaxTrees.Select(st => st.Root);
            var namespaceDeclarations = compilationUnits.SelectMany(st => st.Namespaces).OrderBy(s => s.IdentifierPath.Count);
            var directlyDeclaredNamespaces = new List<(NamespaceSymbol, NamespaceDeclarationSyntax)>();

            foreach (var ns in namespaceDeclarations)
                directlyDeclaredNamespaces.Add(globalBinder.BindNamespaceDeclaration(ns));

            //3. Bind usings
            foreach (var compilationUnit in compilationUnits)
                globalBinder.BindUsings(compilationUnit);

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            //4. Bind all the declarations in every directly declared namespace
            foreach (var ns in directlyDeclaredNamespaces)
            {
                var binder = new Binder(parentScope, null, globalNamespace, ns.Item1);
                binder.BindDeclarationsInNamespace(ns.Item2);
                diagnostics.AddRange(binder.Diagnostics);
            }
                
            //5. Create the global scope
            diagnostics.AddRange(syntaxTrees.SelectMany(d => d.Diagnostics));
            diagnostics.AddRange(globalBinder.Diagnostics);
            
            return new BoundGlobalScope(diagnostics.ToImmutable(), globalNamespace);
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var boundNamespaces = ImmutableDictionary.CreateBuilder<NamespaceSymbol, BoundNamespace>();
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(globalScope.Diagnostics);

            foreach (var ns in globalScope.GlobalNamespace.NestedNamespaces)
            {
                if (!ns.IsBuiltIn)
                {
                    var boundNamespace = BindNamespace(ns, ref diagnostics, globalScope);
                    boundNamespaces.Add(ns, boundNamespace);
                }
            }

            return new BoundProgram(globalScope.GlobalNamespace, diagnostics.ToImmutable(), boundNamespaces.ToImmutable());
        }

        private static BoundNamespace BindNamespace(NamespaceSymbol ns, ref ImmutableArray<Diagnostic>.Builder diagnostics, BoundGlobalScope globalScope)
        {
            var functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundStatement>();
            var childrenBuilder = ImmutableDictionary.CreateBuilder<NamespaceSymbol, BoundNamespace>();
            var parentScope = new BoundScope(null);

            foreach (var function in ns.Functions)
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

            foreach (var child in ns.NestedNamespaces)
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

                    //TODO: Add warning diagnostics for duplicate usings
                    if (seenUsings.Contains(namespaceToUse))                      
                        continue;
  
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

        private (NamespaceSymbol, NamespaceDeclarationSyntax) BindNamespaceDeclaration(NamespaceDeclarationSyntax ns)
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
                    Debug.Assert(found != null);

                    if (i + 1 == identifierPath.Count)
                    {
                        _diagnostics.ReportNamespaceAlreadyDeclared(identifierPath.Last().Location, found.GetFullName());
                        break;
                    }
                    else
                    {   
                        currentNamespace = found;
                    }
                }
                else
                {
                    currentNamespace.TryDeclareNested(newNamespace);
                    currentNamespace = newNamespace;
                }
            }

            return (currentNamespace, ns);

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

        private void BindDeclarationsInNamespace(NamespaceDeclarationSyntax declarationSyntax)
        {
            foreach (var member in declarationSyntax.Members)
            {
                if (member is FunctionDeclarationSyntax functionDeclaration)
                    BindFunctionDeclaration(functionDeclaration);
                else if (member is FieldDeclarationSyntax fieldDeclaration)
                    BindFieldDeclaration(fieldDeclaration);
                else if (member is EnumDeclarationSyntax enumDeclaration)
                    BindEnumDeclaration(enumDeclaration);
            }
        }
             
        private void BindFunctionDeclaration(FunctionDeclarationSyntax declaration)
        {
            var identifierText = declaration.Identifier.Text;

            if (identifierText.ToLower() != identifierText)
                _diagnostics.ReportUpperCaseInFunctionName(declaration.Identifier.Location, identifierText);

            var isTick = false;
            var isLoad = false;

            foreach (var modifier in declaration.Modifiers)
            {
                if (modifier.Kind == SyntaxKind.LoadKeyword)
                {
                    if (_namespace.LoadFunction != null)
                    {
                        _diagnostics.ReportSecondLoadFunction(modifier.Location, _namespace.GetFullName());
                        return;
                    }
                    else
                        isLoad = true;
                }
                else if (modifier.Kind == SyntaxKind.TickKeyword)
                {
                    if (_namespace.TickFunction != null)
                    {
                        _diagnostics.ReportSecondTickFunction(modifier.Location, _namespace.GetFullName());
                        return;
                    }
                    else
                        isTick = true;
                }
            }

            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
            var seenParameterNames = new HashSet<string>();

            if (declaration.Parameters.Count != 0)
            {
                if (isLoad)
                {
                    _diagnostics.ReportLoadFunctionWithParameters(declaration.Parameters.First().Location);
                    return;
                }
                else if (isTick)
                {
                    _diagnostics.ReportTickFunctionWithParameters(declaration.Parameters.First().Location);
                    return;
                }
            }

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

            TypeSymbol? returnType;
            if (declaration.ReturnTypeClause == null)
                returnType = TypeSymbol.Void;
            else
            {
                var identifier = declaration.ReturnTypeClause.Identifier;
                returnType = BindType(identifier.Text, identifier.Location);

                if (returnType == null)
                    returnType = TypeSymbol.Void;
                else if (returnType is NamedTypeSymbol)
                {
                    _diagnostics.ReportReturningNamedType(declaration.ReturnTypeClause.Location);
                }
            }

            var function = new FunctionSymbol(identifierText, _namespace, parameters.ToImmutable(), returnType, isLoad, isTick, declaration);

            if (!_namespace.TryDeclareFunction(function))
                _diagnostics.ReportFunctionAlreadyDeclared(declaration.Identifier.Location, function.Name);
        }

        private void BindFieldDeclaration(FieldDeclarationSyntax fieldDeclaration)
        {
            var identifierText = fieldDeclaration.Identifier.Text;
            var initializer = BindExpression(fieldDeclaration.Initializer);

            TypeSymbol? type = null;
            if (fieldDeclaration.DeclarationNode is TypeClauseSyntax typeClause)
                type = BindType(typeClause.Identifier.Text, typeClause.Identifier.Location);

            var variableType = type ?? initializer.Type;

            var field = _namespace.Fields.FirstOrDefault(f => f.Name == identifierText);
            if (field != null)
            {
                _diagnostics.ReportFieldAlreadyDeclared(fieldDeclaration.Identifier.Location, identifierText);
            }
            else
            {
                var convertedInitializer = BindConversion(initializer, variableType, fieldDeclaration.Initializer.Location);
                field = new FieldSymbol(identifierText, _namespace, variableType, convertedInitializer);
                _namespace.Members.Add(field);
            }
        }

        private void BindEnumDeclaration(EnumDeclarationSyntax enumDeclaration)
        {
            var identifierText = enumDeclaration.Identifier.Text;

            var declaredEnum = _namespace.Enums.FirstOrDefault();
            if (declaredEnum != null)
            {
                _diagnostics.ReportEnumAlreadyDeclared(enumDeclaration.Identifier.Location, identifierText);
            }
            else
            {
                declaredEnum = new EnumSymbol(_namespace, identifierText);
                _namespace.Members.Add(declaredEnum);

                var seenNames = new HashSet<string>();

                for (int i = 0; i < enumDeclaration.MemberDeclarations.Length; i++)
                {
                    var memberDeclaration = enumDeclaration.MemberDeclarations[i];
                    var memberName = memberDeclaration.Identifier.Text;

                    if (seenNames.Contains(memberName))
                    {
                        _diagnostics.ReportEnumMemberAlreadyDeclared(memberDeclaration.Location, memberName, identifierText);
                    }
                    else
                    {
                        var declaredMember = new EnumMemberSymbol(declaredEnum, memberName, i);
                        declaredEnum.Members.Add(declaredMember);
                        seenNames.Add(memberName);
                    }
                }
            }
        }

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

            //TODO: Add const variable declarations
            var variableType = type ?? initializer.Type;
            var variable = BindVariable(syntax.Identifier, variableType, false, initializer.ConstantValue);
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

            var variable = BindVariable(syntax.Identifier, TypeSymbol.Int, false);
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

        private BoundExpression BindExpression(ExpressionSyntax expression, bool canBeVoid = false, LookupOptions lookupOptions = LookupOptions.PrioritizeVariables)
        {
            var result = BindExpressionInternal(expression, lookupOptions);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(expression.Location);
                return new BoundErrorExpression();
            }
            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax expression, LookupOptions options)
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

                SyntaxKind.SimpleNameExpression => BindSimpleNameExpression((SimpleNameExpressionSyntax)expression, options),
                SyntaxKind.CallExpression => BindCallExpression((CallExpressionSyntax)expression),
                SyntaxKind.MemberAccessExpression => BindMemberAccessExpression((MemberAccessExpressionSyntax)expression, options),
    
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

        private BoundExpression BindSimpleNameExpression(SimpleNameExpressionSyntax expression, LookupOptions options)
        {
            var name = expression.IdentifierToken.Text;
            if (expression.IdentifierToken.IsMissingText)
                return new BoundErrorExpression();

            var boundExpression = BindSymbolExpression(name, _namespace, options);
            
            if (boundExpression == null)
            {
                _diagnostics.ReportUndefinedName(expression.IdentifierToken.Location, name);
                return new BoundErrorExpression();
            }

            if (_function == null && !(boundExpression is BoundTypeExpression))
            {
                _diagnostics.ReportInvalidFieldIdentifier(expression.Location, expression.Kind);
                return new BoundErrorExpression();
            }

            return boundExpression;
        }

        private BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax expression, LookupOptions lookupOptions)
        {
            if (_function == null)
            {
                _diagnostics.ReportInvalidFieldIdentifier(expression.Location, expression.Kind);
                return new BoundErrorExpression();
            }

            if (!expression.AccessedExpression.CanBeAccessed())
            {
                _diagnostics.ReportInvalidMemberAccessLeftKind(expression.AccessedExpression.Location, expression.AccessedExpression.Kind);
                return new BoundErrorExpression();
            }

            var boundAccessed = BindExpression(expression.AccessedExpression, true, LookupOptions.PrioritizeVariables);
            var memberName = expression.MemberIdentifier.Text;

            if (boundAccessed.Type.IsError)
                return boundAccessed;

            if (boundAccessed.Type is NamedTypeSymbol namedType)
            {
                //Named type members -> Fields, Functions
                Symbol? foundMember;

                if (lookupOptions == LookupOptions.PrioritizeFunctions)
                {
                    foundMember = namedType.TryLookup<FunctionSymbol>(memberName);
                    if (foundMember != null)
                        return new BoundMethodAccessExpression(boundAccessed, (FunctionSymbol)foundMember);

                    foundMember = namedType.TryLookup<FieldSymbol>(memberName);
                    if (foundMember != null)
                        return new BoundFieldAccessExpression(boundAccessed, (FieldSymbol)foundMember);
                }
                else
                {
                    foundMember = namedType.TryLookup<FieldSymbol>(memberName);
                    if (foundMember != null)
                        return new BoundFieldAccessExpression(boundAccessed, (FieldSymbol)foundMember);

                    foundMember = namedType.TryLookup<FunctionSymbol>(memberName);
                    if (foundMember != null)
                        return new BoundMethodAccessExpression(boundAccessed, (FunctionSymbol)foundMember);
                }

                _diagnostics.ReportUndefinedMemberOfType(expression.MemberIdentifier.Location, namedType.Name, memberName);
                return new BoundErrorExpression();                
            }
            else if (boundAccessed is BoundNamespaceExpression namespaceExpression)
            {
                //Use standard namespace symbol binding for this one,
                //With the context namespace being the one in the expression

                var boundSymbolExression = BindSymbolExpression(memberName, namespaceExpression.Namespace, lookupOptions);

                if (boundSymbolExression == null)
                {
                    _diagnostics.ReportUndefinedMemberOfNamespace(expression.MemberIdentifier.Location, namespaceExpression.Namespace.Name, memberName);
                    return new BoundErrorExpression();
                }
                return boundSymbolExression;
            }
            else if (boundAccessed is BoundEnumExpression enumExpression)
            {
                //Enum types -> Enum members
                var member = enumExpression.EnumSymbol.TryLookup(memberName);
                if (member == null)
                {
                    _diagnostics.ReportUndefinedEnumMember(expression.MemberIdentifier.Location, enumExpression.EnumSymbol.Name, memberName);
                    return new BoundErrorExpression();
                }
                return new BoundVariableExpression(member);
            }
            else
            {
                _diagnostics.ReportInvalidMemberAccess(expression.AccessedExpression.Location, boundAccessed.Type.Name);
                return new BoundErrorExpression();
            }
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax expression)
        {
            if (_function == null)
            {
                _diagnostics.ReportInvalidFieldIdentifier(expression.Location, expression.Kind);
                return new BoundErrorExpression();
            }

            var boundIdentifier = BindExpression(expression.IdentifierExpression, true, LookupOptions.PrioritizeFunctions);
            FunctionSymbol function;

            if (boundIdentifier is BoundErrorExpression errorExpression)
                return errorExpression;

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
            var boundIdentifier = BindExpression(syntax.Identifier, false, LookupOptions.PrioritizeNamedTypes);

            if (boundIdentifier is BoundErrorExpression)
                return boundIdentifier;

            if (!(boundIdentifier is BoundTypeExpression typeExpression))
            {
                _diagnostics.ReportInvalidObjectCreationIdentifier(syntax.Identifier.Location, boundIdentifier.Kind);
                return new BoundErrorExpression();
            }

            if (typeExpression.Type is NamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.Constructor.Parameters.Length != syntax.Arguments.Count)
                {
                    _diagnostics.ReportWrongConstructorArgumentCount(syntax.Location, namedTypeSymbol.Name, namedTypeSymbol.Constructor.Parameters.Length, syntax.Arguments.Count);
                    return new BoundErrorExpression();
                }

                var arguments = BindArguments(syntax.Arguments, namedTypeSymbol.Constructor.Parameters);
                return new BoundObjectCreationExpression(namedTypeSymbol, arguments);
            }
            else throw new Exception("Value type in object creation expression");
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
            var boundLeft = BindExpression(expression.Left, false, LookupOptions.PrioritizeVariables);
            var boundRight = BindExpression(expression.Right);

            if (boundLeft is BoundVariableExpression variableExpression)
            {
                if (variableExpression.Variable.IsReadOnly)
                {
                    _diagnostics.ReportAssigningToReadOnly(expression.Left.Location, boundLeft.Kind);
                    return new BoundErrorExpression();
                }
            }
            else if (boundLeft is BoundFieldAccessExpression fieldAccessExpression)
            {
                if (fieldAccessExpression.Field.IsReadOnly)
                {
                    _diagnostics.ReportAssigningToReadOnly(expression.Left.Location, boundLeft.Kind);
                    return new BoundErrorExpression();
                }
            }
            else 
            {
                if (boundLeft.Kind != BoundNodeKind.ErrorExpression)
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
            var boundLeft = BindExpression(expression.Expression, false, LookupOptions.PrioritizeVariables);
            
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

        private VariableSymbol BindVariable(SyntaxToken identifier, TypeSymbol type, bool isReadOnly, BoundConstant? constant = null)
        {
            var name = identifier.Text;
            VariableSymbol variable = _function == null
                                ? new GlobalVariableSymbol(name, type, isReadOnly, constant)
                                : new LocalVariableSymbol(name, type, isReadOnly, constant);

            if (_namespace.TryLookup<FieldSymbol>(name) != null)
                _diagnostics.ReportVariableNameIsADeclaredField(identifier.Location, name);

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
            if (type == null)
                type = _namespace.TryLookup<EnumSymbol>(name);

            return type;
        }

        private BoundExpression? BindSymbolExpression(string name, NamespaceSymbol context, LookupOptions options)
        {
            var symbol = LookupSymbol(name, context, options);

            if (symbol == null)
                return null;

            if (symbol is FieldSymbol field)
            {
                var parentNamespace = (NamespaceSymbol) field.Parent;
                var namespaceExpression = new BoundNamespaceExpression(parentNamespace);
                return new BoundFieldAccessExpression(namespaceExpression, field);
            }

            if (symbol is VariableSymbol variable)
                return new BoundVariableExpression(variable);

            if (symbol is EnumMemberSymbol enumSymbol)
                return new BoundVariableExpression(enumSymbol);

            if (symbol is EnumSymbol en)
                return new BoundEnumExpression(en);

            if (symbol is TypeSymbol type)
                return new BoundTypeExpression(type);

            if (symbol is NamespaceSymbol ns)
                return new BoundNamespaceExpression(ns);

            if (symbol is FunctionSymbol function)
                return new BoundFunctionExpression(function);

            return null;
        }

        private Symbol? LookupSymbol(string name, NamespaceSymbol context, LookupOptions options)
        {
            //Options simply specify the order in which the members are lookup up
            //This is quite dirty but I can't think of another way to do this

            TypeSymbol? type = TypeSymbol.Lookup(name);
            if (type != null)
                return type;

            switch (options)
            {
                case LookupOptions.PrioritizeFunctions:
                    {
                        var function = context.TryLookup<FunctionSymbol>(name);
                        if (function != null)
                            return function;

                        var field = context.TryLookup<FieldSymbol>(name);
                        if (field != null)
                            return field;

                        var variable = _scope.TryLookupVariable(name);
                        if (variable != null)
                            return variable;

                        var namedType = context.TryLookup<NamedTypeSymbol>(name);
                        if (namedType != null)
                            return namedType;

                        var nestedNamespace = context.TryLookupDirectChildOrUsed(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;

                        nestedNamespace = _globalNamespace.TryLookupDirectChild(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;

                        var enumSymbol = context.TryLookup<EnumSymbol>(name);
                        if (enumSymbol != null)
                            return enumSymbol;
                    }
                    break;
                case LookupOptions.PrioritizeNamedTypes:
                    {
                        var namedType = context.TryLookup<NamedTypeSymbol>(name);
                        if (namedType != null)
                            return namedType;

                        var enumSymbol = context.TryLookup<EnumSymbol>(name);
                        if (enumSymbol != null)
                            return enumSymbol;

                        var function = context.TryLookup<FunctionSymbol>(name);
                        if (function != null)
                            return function;

                        var field = context.TryLookup<FieldSymbol>(name);
                        if (field != null)
                            return field;

                        var variable = _scope.TryLookupVariable(name);
                        if (variable != null)
                            return variable;

                        var nestedNamespace = context.TryLookupDirectChildOrUsed(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;

                        nestedNamespace = _globalNamespace.TryLookupDirectChild(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;
                    }
                    break;
                case LookupOptions.PrioritizeVariables:
                    {
                        var field = context.TryLookup<FieldSymbol>(name);
                        if (field != null)
                            return field;

                        var variable = _scope.TryLookupVariable(name);
                        if (variable != null)
                            return variable;

                        var namedType = context.TryLookup<NamedTypeSymbol>(name);
                        if (namedType != null)
                            return namedType;

                        var enumSymbol = context.TryLookup<EnumSymbol>(name);
                        if (enumSymbol != null)
                            return enumSymbol;

                        var function = context.TryLookup<FunctionSymbol>(name);
                        if (function != null)
                            return function;

                        var nestedNamespace = context.TryLookupDirectChildOrUsed(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;

                        nestedNamespace = _globalNamespace.TryLookupDirectChild(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;
                    }
                    break;
                default:
                    {
                        var nestedNamespace = context.TryLookupDirectChildOrUsed(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;

                        nestedNamespace = _globalNamespace.TryLookupDirectChild(name);
                        if (nestedNamespace != null)
                            return nestedNamespace;

                        var field = context.TryLookup<FieldSymbol>(name);
                        if (field != null)
                            return field;

                        var variable = _scope.TryLookupVariable(name);
                        if (variable != null)
                            return variable;

                        var enumSymbol = context.TryLookup<EnumSymbol>(name);
                        if (enumSymbol != null)
                            return enumSymbol;

                        var namedType = context.TryLookup<NamedTypeSymbol>(name);
                        if (namedType != null)
                            return namedType;

                        var function = context.TryLookup<FunctionSymbol>(name);
                        if (function != null)
                            return function;
                    }
                    break;
            }
            return null;
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