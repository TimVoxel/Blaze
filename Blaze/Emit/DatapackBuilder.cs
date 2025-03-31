using Blaze.Binding;
using Blaze.Emit.Data;
using Blaze.Emit.Nodes;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Text;
using static Blaze.Emit.Nodes.Execute.StoreExecuteSubCommand;

namespace Blaze.Emit
{
    internal sealed class DatapackBuilder
    {
        private const string CONST = "CONST";

        private static readonly Coordinates2 _debugChunk = new Coordinates2("10000000", "10000000");
        private static readonly UUID _mathEntity1 = new UUID(1068730519, 377069937, 1764794166, -1230438844);
        private static readonly UUID _mathEntity2 = new UUID(-1824770608, 1852200875, -1037488134, 520770809);
        private static readonly EmittionVariableSymbol _macro = new EmittionVariableSymbol(MacroEmittionVariableSymbol.MACRO_PREFIX, TypeSymbol.Object, true);
        private static readonly EmittionVariableSymbol _returnValue = new EmittionVariableSymbol("*return.value", TypeSymbol.Object, false);
        
        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;

        private readonly MinecraftFunction.Builder _initFunction;
        private readonly MinecraftFunction.Builder _tickFunction;

        private readonly List<FunctionSymbol> _fabricatedMacroFunctions = new List<FunctionSymbol>();
        private readonly Dictionary<FunctionSymbol, MinecraftFunction.Builder> _usedBuiltIn = new Dictionary<FunctionSymbol, MinecraftFunction.Builder>();

        private readonly CommandNodeFactory _factory;

        private EmittionVariableSymbol? _thisSymbol = null;

        public string Vars => $"{_configuration.RootNamespace}.vars";
        public string MainStorage => $"{_configuration.RootNamespace}:main";
        
        public DatapackBuilder(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _configuration = configuration;

            _initFunction = MinecraftFunction.Init(_configuration.RootNamespace, program.GlobalNamespace);
            _tickFunction = MinecraftFunction.Tick(_configuration.RootNamespace, program.GlobalNamespace);
            _factory = new CommandNodeFactory(_configuration.RootNamespace, Vars, MainStorage, CONST, _macro);
        }


        private CommandNode GetMacroCall(MinecraftFunction.Builder function) => _factory.GetMacroCall(function);
        private CommandNode GetCall(MinecraftFunction.Builder function) => _factory.GetCall(function);
        private CommandNode GetCall(FunctionSymbol function) => _factory.GetCall(function);
        private CommandNode ScoreSet(EmittionVariableSymbol variable, int value) => _factory.ScoreSet(variable, value);
        private CommandNode ScoreGet(EmittionVariableSymbol variable, string? multiplier = null) => _factory.ScoreGet(variable, multiplier);

        private CommandNode DataSetValue(EmittionVariableSymbol symbol, string value) => _factory.DataSetValue(symbol, value);
        private CommandNode DataStringCopy(EmittionVariableSymbol left, EmittionVariableSymbol right, int? startIndex = null, int? endIndex = null) => _factory.DataStringCopy(left, right, startIndex, endIndex);

        private CommandNodeFactory.ExecuteCommandBuilder Execute() => _factory.CreateExecuteBuilder();
        private TextTriviaNode LineBreak() => TextTriviaNode.LineBreak();
        private TextTriviaNode Comment(string comment) => TextTriviaNode.Comment(comment);

        public Datapack BuildDatapack()
        {
            AddInitializationCommands();
            _initFunction.EnterNewScope();

            var namespaceEmittionBuilder = ImmutableArray.CreateBuilder<NamespaceEmittionNode>();

            foreach (var ns in _program.Namespaces)
            {
                var namespaceSymbol = ns.Key;
                var boundNamespace = ns.Value;

                var namespaceEmittion = GetNamespace(namespaceSymbol, boundNamespace);
                namespaceEmittionBuilder.Add(namespaceEmittion);
            }

            foreach (var ns in _program.GlobalNamespace.NestedNamespaces)
            {
                if (ns.IsBuiltIn)
                {
                    var emittion = GetBuiltInNamespace(ns);
                    if (emittion != null)
                        namespaceEmittionBuilder.Add(emittion);
                }
            }

            _initFunction.AddLineBreak();

            foreach (var namespaceEmittion in namespaceEmittionBuilder)
            {
                if (namespaceEmittion.LoadFunction != null)
                {
                    Debug.Assert(namespaceEmittion.LoadFunction.Symbol != null);
                    var functionSymbol = (FunctionSymbol) namespaceEmittion.LoadFunction.Symbol;
                    _initFunction.Content.Add(GetCall(functionSymbol));
                }
                if (namespaceEmittion.TickFunction != null)
                {
                    Debug.Assert(namespaceEmittion.TickFunction.Symbol != null);
                    var functionSymbol = (FunctionSymbol)namespaceEmittion.TickFunction.Symbol;
                    _tickFunction.Content.Add(GetCall(functionSymbol));
                }
            }

            _initFunction.AddLineBreak();
            _initFunction.ExitScope();
            _initFunction.Content.Add(GetCleanUp(_initFunction));

            var initFunction = _initFunction.ToFunction();
            var tickFunction = _tickFunction.ToFunction();
            var datapack = new Datapack(namespaceEmittionBuilder.ToImmutable(), _configuration, _program.Diagnostics, initFunction, tickFunction);
            return datapack;
        }

        private void AddInitializationCommands()
        {
            _initFunction.AddComment("Blaze setup");
            _initFunction.AddCommand(ScoreboardCommand.AddObjective(Vars, "dummy"));
            _initFunction.AddCommand(ScoreboardCommand.AddObjective(CONST, "dummy"));
            _initFunction.Content.Add(ScoreSet(new EmittionVariableSymbol("-1", TypeSymbol.Int, false), -1));

            //Debug chunk setup
            _initFunction.AddLineBreak();
            _initFunction.AddCommand(new ForceloadCommand(ForceloadCommand.SubAction.Add, _debugChunk));
            _initFunction.AddCommand(new KillCommnad("@e[tag=debug,tag=blz]"));

            var coords = new Coordinates3(_debugChunk.X, "0", _debugChunk.Z);
            _initFunction.AddCommand(new SummonCommand("item_display", coords, $"{{Tags:[\"blz\",\"debug\",\"first\"], UUID:{_mathEntity1.TagValue}, item:{{id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:1,less:0}}}}}}}}"));
            _initFunction.AddCommand(new SummonCommand("item_display", coords, $"{{Tags:[\"blz\",\"debug\", \"second\"], UUID:{_mathEntity2.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:0,less:1}}}}}}}}"));
        }

        private string GetEmittionVariableName(VariableSymbol variable)
        {
            if (variable is ParameterSymbol p)
                return $"*{p.FunctionHash:X8}{variable.Name}";
            else
                return variable.Name;
        }

        private EmittionVariableSymbol ToEmittionVariable(MinecraftFunction.Builder functionBuilder, VariableSymbol variable, bool makeTemp, bool useScoping, bool enforceNew = false)
        {
            var name = GetEmittionVariableName(variable);
            
            if (enforceNew)
                 return functionBuilder.Scope.Declare(name, variable.Type, makeTemp);
            else 
                return functionBuilder.Scope.LookupOrDeclare(name, variable.Type, makeTemp, useScoping, null);
        }

        private TextEmittionNode Block(params TextEmittionNode[] nodes)
        {
            //TODO: improve rubbish removal

            var nonTrivia = nodes.Where(n => !(n is TextTriviaNode));

            if (nonTrivia.Count() == 1)
                return nonTrivia.First();
            else
                return new TextBlockEmittionNode(nodes.ToImmutableArray());
        }

        private NamespaceEmittionNode? GetBuiltInNamespace(NamespaceSymbol ns, NamespaceEmittionNode? parent = null)
        {
            //We do this so that we do not generate unused functions and folders

            ImmutableArray<StructureEmittionNode>.Builder? childrenBuilder = null;

            foreach (var function in ns.Functions)
            {
                if (_usedBuiltIn.ContainsKey(function))
                {
                    if (childrenBuilder == null)
                        childrenBuilder = ImmutableArray.CreateBuilder<StructureEmittionNode>();

                    var emittion = _usedBuiltIn[function].ToFunction();
                    childrenBuilder.Add(emittion);
                }
            }

            foreach (var child in ns.NestedNamespaces)
            {
                var emittion = GetBuiltInNamespace(child, parent);
                if (emittion != null)
                {
                    if (childrenBuilder == null)
                        childrenBuilder = ImmutableArray.CreateBuilder<StructureEmittionNode>();

                    childrenBuilder.Add(emittion);
                }
            }

            NamespaceEmittionNode? result = null;

            if (childrenBuilder != null || childrenBuilder != null)
            {
                var children = childrenBuilder == null ? ImmutableArray<StructureEmittionNode>.Empty : childrenBuilder.ToImmutable();
                result = new NamespaceEmittionNode(ns, ns.Name, children, null, null);
            }
            return result;
        }

        private NamespaceEmittionNode GetNamespace(NamespaceSymbol symbol, BoundNamespace boundNamespace)
        {
            var childrenBuilder = ImmutableArray.CreateBuilder<StructureEmittionNode>();
            var ns = new BoundNamespaceExpression(symbol);

            MinecraftFunction? loadFunction = null;
            MinecraftFunction? tickFunction = null;

            foreach (var field in symbol.Fields)
            {
                if (field.Initializer == null)
                    continue;

                var fieldAccess = new BoundFieldAccessExpression(ns, field);
                _initFunction.Content.Add(GetAssignment(_initFunction, fieldAccess, field.Initializer, 0));
            }

            foreach (var function in boundNamespace.Functions)
            {
                var functionEmittion = GetFunction(function.Key, function.Value);

                if (function.Key.IsLoad)
                    loadFunction = functionEmittion;
                if (function.Key.IsTick)
                    tickFunction = functionEmittion;

                childrenBuilder.Add(functionEmittion);
            }

            foreach (var child in boundNamespace.Children)
            {
                var nestedNamespace = GetNamespace(child.Key, child.Value);
                childrenBuilder.Add(nestedNamespace);
            }

            var namespaceEmittion = new NamespaceEmittionNode(symbol, symbol.Name, childrenBuilder.ToImmutable(), loadFunction, tickFunction);
            return namespaceEmittion;
        }

        private MinecraftFunction GetFunction(FunctionSymbol function, BoundStatement bodyBlock)
        {
            var functionBuilder = new MinecraftFunction.Builder(function.Name, _configuration.RootNamespace, null, function, null);

            if (function.ReturnType != TypeSymbol.Void)
                functionBuilder.Content.Add(GetResetCommand(_returnValue));

            var body = GetStatement(functionBuilder, bodyBlock);
            functionBuilder.Content.Add(body);

            if (functionBuilder.Function.ReturnType == TypeSymbol.Void)
                functionBuilder.Content.Add(GetCleanUp(functionBuilder));

            return functionBuilder.ToFunction();
        }

        private TextEmittionNode GetCleanUp(MinecraftFunction.Builder functionBuilder, ImmutableArray<EmittionVariableSymbol>? extras = null)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            
            foreach (var local in functionBuilder.Locals)
                if (local.IsTemp)
                    builder.Add(GetResetCommand(local));
            
            if (builder.Any())
            {
                builder.Insert(0, Comment("Clean up"));
                return new TextBlockEmittionNode(builder.ToImmutable());
            }
            else
                return TextTriviaNode.Empty();
        }

        private TextEmittionNode GetResetCommand(EmittionVariableSymbol local)
        {
            return local.Location switch
            {
                DataLocation.Scoreboard => _factory.ScoreReset(local),
                DataLocation.Storage => _factory.DataRemove(local),
                _ => throw new Exception($"Unexpected variable location {local.Location}"),
            };
        }

        private TextEmittionNode GetStatement(MinecraftFunction.Builder functionBuilder, BoundStatement node)
        {
            return node.Kind switch
            {
                BoundNodeKind.NopStatement => GetNopStatement(),
                BoundNodeKind.BlockStatement => GetBlockStatement(functionBuilder, (BoundBlockStatement)node),
                BoundNodeKind.ExpressionStatement => GetExpressionStatement(functionBuilder, (BoundExpressionStatement)node),
                BoundNodeKind.VariableDeclarationStatement => GetVariableDeclarationStatement(functionBuilder, (BoundVariableDeclarationStatement)node),
                BoundNodeKind.IfStatement => GetIfStatement(functionBuilder, (BoundIfStatement)node),
                BoundNodeKind.WhileStatement => GetWhileStatement(functionBuilder, (BoundWhileStatement)node),
                BoundNodeKind.DoWhileStatement => GetDoWhileStatement(functionBuilder, (BoundDoWhileStatement)node),
                BoundNodeKind.BreakStatement => GetBreakStatement(functionBuilder, (BoundBreakStatement)node),
                BoundNodeKind.ContinueStatement => GetContinueStatement(functionBuilder, (BoundContinueStatement)node),
                BoundNodeKind.ReturnStatement => GetReturnStatement(functionBuilder, (BoundReturnStatement)node),
                _ => throw new Exception($"Unexpected node {node.Kind}"),
            };
        }

        private TextEmittionNode GetBlockStatement(MinecraftFunction.Builder functionBuilder, BoundBlockStatement node)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            functionBuilder.EnterNewScope();

            foreach (var statement in node.Statements)
                builder.Add(GetStatement(functionBuilder, statement));

            functionBuilder.ExitScope();

            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode GetVariableDeclarationStatement(MinecraftFunction.Builder functionBuilder, BoundVariableDeclarationStatement node)
        {
            var variable = ToEmittionVariable(functionBuilder, node.Variable, false, true, true);
            return GetAssignment(functionBuilder, variable, node.Initializer, 0);
        }

        private TextEmittionNode GetIfStatement(MinecraftFunction.Builder functionBuilder, BoundIfStatement node)
        {
            var subFunction = functionBuilder.CreateSub(SubFunctionKind.If);
            subFunction.Content.Add(GetStatement(subFunction, node.Body));

            var temp = Temp(functionBuilder, TypeSymbol.Bool, 0);
            
            if (node.ElseBody == null)
            {
                return Block(
                        GetAssignment(functionBuilder, temp, node.Condition, 0),
                        Execute().IfScoreMatches(temp, "1").Run(GetCall(subFunction))
                    );
            }
            else
            {
                var elseSubFunction = functionBuilder.CreateSub(SubFunctionKind.Else);
                elseSubFunction.Content.Add(GetStatement(elseSubFunction, node.ElseBody));

                return Block(
                        GetAssignment(functionBuilder, temp, node.Condition, 0),
                        Execute().IfScoreMatches(temp, "1").Run(GetCall(subFunction)),
                        Execute().IfScoreMatches(temp, "0").Run(GetCall(elseSubFunction))
                    );
            }
        }

        private TextEmittionNode GetWhileStatement(MinecraftFunction.Builder functionBuilder, BoundWhileStatement node)
        {
            var subFunction = functionBuilder.CreateSub(SubFunctionKind.Loop);
            var temp = Temp(functionBuilder, TypeSymbol.Bool, 0);

            var callCommand = GetCall(subFunction);

            subFunction.Content.Add(GetAssignment(subFunction, temp, node.Condition));
            subFunction.Content.Add(Execute().IfScoreMatches(temp, "0").Run(new ReturnValueCommand("0")));
            subFunction.AddLineBreak();
            subFunction.Content.Add(GetStatement(subFunction, node.Body));
            subFunction.AddLineBreak();
            subFunction.Content.Add(callCommand);

            return callCommand;
        }

        private TextEmittionNode GetDoWhileStatement(MinecraftFunction.Builder functionBuilder, BoundDoWhileStatement node)
        {
            var subFunction = functionBuilder.CreateSub(SubFunctionKind.Loop);
            var temp = Temp(functionBuilder, TypeSymbol.Bool, 0);

            var callCommand = GetCall(subFunction);

            subFunction.Content.Add(GetAssignment(subFunction, temp, node.Condition, 0));
            subFunction.AddLineBreak();
            subFunction.Content.Add(GetStatement(subFunction, node.Body));
            subFunction.Content.Add(Execute().IfScoreMatches(temp, "1").Run(callCommand));
            
            return callCommand;
        }

        private TextEmittionNode GetContinueStatement(MinecraftFunction.Builder functionBuilder, BoundContinueStatement node)
        {
            throw new NotImplementedException();
        }

        private TextEmittionNode GetBreakStatement(MinecraftFunction.Builder functionBuilder, BoundBreakStatement node)
        {
            throw new NotImplementedException();
        }

        private TextEmittionNode GetReturnStatement(MinecraftFunction.Builder functionBuilder, BoundReturnStatement node)
        {
            var returnExpression = node.Expression;
            functionBuilder.Locals.AddRange(functionBuilder.Scope.GetLocals());

            if (returnExpression == null)
                return Block(
                    GetCleanUp(functionBuilder, functionBuilder.Scope.GetLocals()),
                    new ReturnValueCommand("0")
                );
            
            return Block(
                    GetCleanUp(functionBuilder, functionBuilder.Scope.GetLocals()),
                    GetAssignment(functionBuilder, _returnValue, returnExpression, 0),
                    new ReturnRunCommand(_factory.StorageGet(_returnValue))
                );
        }

        private TextEmittionNode GetExpressionStatement(MinecraftFunction.Builder functionBuilder, BoundExpressionStatement node)
        {
            var expression = node.Expression;

            if (expression is BoundAssignmentExpression assignment)
                return GetAssignment(functionBuilder, assignment, 0);
            else if (expression is BoundCallExpression call)
                return GetCallExpression(functionBuilder, null, call, 0);
            else
                throw new Exception($"Unexpected expression statement kind {expression.Kind}");
        }

        private TextEmittionNode GetCallExpression(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol? symbol, BoundCallExpression call, int current)
        {
            if (TryGetBuiltInFunctionEmittion(functionBuilder, symbol, call, current, out var node))
            {
                Debug.Assert(node != null);
                return node;
            }
               
            return Block(
                    GetArgumentAssignment(functionBuilder, call.Function, call.Function.Parameters, call.Arguments),
                    GetCall(call.Function)
                );
        }

        private TextEmittionNode GetNopStatement() => new TellrawCommand("@a", "{\"text\":\"Nop statement in program\", \"color\":\"red\"}");

        private TextEmittionNode GetAssignment(MinecraftFunction.Builder functionBuilder, BoundAssignmentExpression assignment, int current)
            => GetAssignment(functionBuilder, assignment.Left, assignment.Right, current);

        private TextEmittionNode GetAssignment(MinecraftFunction.Builder functionBuilder, BoundExpression left, BoundExpression right, int tempIndex)
        {
            if (left is BoundFieldAccessExpression fieldAccess)
            {
                if (TrygetBuiltInFieldAssignment(functionBuilder, fieldAccess.Field, right, tempIndex, out var node))
                {
                    Debug.Assert(node != null);
                    return node;
                }
            }

            if (left is BoundVariableExpression v)
            {
                var leftVar = ToEmittionVariable(functionBuilder, v.Variable, false, true);
                return GetAssignment(functionBuilder, leftVar, right, tempIndex);
            }

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            builder.Add(Comment($"Assigning \"{left}\" to \"{right}\""));

            var leftAssociativeOrder = GetLeftAssociativeOrder(left);
            var nameYield = new StringBuilder();
            var useMacroAssignment = false;
            var enforceStorage = false;

            while (leftAssociativeOrder.Any())
            {
                var current = leftAssociativeOrder.Pop();

                if (current is BoundMethodAccessExpression || current is BoundFunctionExpression)
                    continue;

                //Requires emittion
                if (current is BoundArrayAccessExpression arrayAccess)
                {
                    builder.Add(LineBreak());
                    enforceStorage = true;
                    useMacroAssignment = true;

                    var yield = nameYield.ToString();
                    builder.Add(GetArrayAccessAssignment(functionBuilder, yield, isSetter: true, arrayAccess, tempIndex));
                    nameYield.Clear();
                    continue;
                }
                else if (current is BoundCallExpression call)
                {
                    enforceStorage = true;
                    builder.Add(GetCallExpression(functionBuilder, null, call, tempIndex));
                    nameYield.Clear();
                    nameYield.Append(_returnValue.Name);
                    continue;
                }
                else
                    nameYield.Append(GetNameAddition(functionBuilder, current, ref enforceStorage));
            }

            if (useMacroAssignment)
            {
                //Assigning right to **macros.right and copying it with a macro
                var macroLeft = Macro(functionBuilder, "left");

                if (nameYield.Length > 0)
                {
                    var literal = new BoundLiteralExpression(nameYield.ToString());
                    builder.Add(GetStringConcatenation(functionBuilder, macroLeft, literal, tempIndex));
                }

                builder.Add(GetMacroAssignment(functionBuilder, macroLeft, right, tempIndex));
                return new TextBlockEmittionNode(builder.ToImmutable());
            }
            else
            {
                //Left expression did not require macro emittion so we just lookup a symbol and assign right to it

                var resultName = nameYield.ToString();
                var leftVar = functionBuilder.Scope.LookupOrDeclare(resultName, left.Type, false, false, enforceStorage ? DataLocation.Storage : null);
                builder.Add(GetAssignment(functionBuilder, leftVar, right, tempIndex));
                return new TextBlockEmittionNode(builder.ToImmutable());
            }
        }

        private TextEmittionNode GetArrayAccessAssignment(MinecraftFunction.Builder functionBuilder, string nameYield, bool isSetter, BoundArrayAccessExpression arrayAccess, int tempIndex)
        {
            var rank = arrayAccess.Arguments.Length;
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var macroName = Macro(functionBuilder, "name");

            builder.Add(GetAssignment(macroName, $"*{nameYield.ToString()}"));

            MinecraftFunction.Builder fabricatedAccessor;

            if (isSetter)
            {
                var macroLeft = Macro(functionBuilder, "left");
                
                if (GetFabricatedFunction($"set_array_access_rank{rank}", TypeSymbol.Void, out fabricatedAccessor))
                {
                    var accessRankBuilder = new StringBuilder();

                    for (int i = 0; i < rank; i++)
                        accessRankBuilder.Append($"[$({"a" + i.ToString()})]");

                    var command = GetAssignment(macroLeft, $"{macroName.Accessor}{accessRankBuilder.ToString()}");
                    fabricatedAccessor.AddMacro(command);
                }
            }
            else
            {
                if (GetFabricatedFunction($"get_array_access_rank{rank}", TypeSymbol.Object, out fabricatedAccessor))
                {
                    var accessRankBuilder = new StringBuilder();

                    for (int i = 0; i < rank; i++)
                        accessRankBuilder.Append($"[$({"a" + i.ToString()})]");

                    var command = _factory.DataModifyFromStorage(_returnValue, DataModifyCommand.ModificationType.Set, $"{macroName.SaveName}{accessRankBuilder.ToString()}");
                    fabricatedAccessor.AddMacro(command);
                }
            }

            for (int i = 0; i < rank; i++)
            {
                var macroArg = Macro(functionBuilder, $"a{i.ToString()}");
                var temp = Temp(functionBuilder, TypeSymbol.Int, tempIndex + i);

                builder.Add(GetAssignment(functionBuilder, temp, arrayAccess.Arguments[i], tempIndex + i));
                builder.Add(Execute()
                            .StoreResultStorage(macroArg, StoreType.Int, "1")
                            .Run(ScoreGet(temp)));
            }

            builder.Add(GetMacroCall(fabricatedAccessor));
            builder.Add(LineBreak());

            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode GetPrimaryAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression right, int tempIndex)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var leftAssociativeOrder = GetLeftAssociativeOrder(right);
            var nameYield = new StringBuilder();
            var useMacroAssignment = false;
            var enforceStorage = false;

            while (leftAssociativeOrder.Any())
            {
                var current = leftAssociativeOrder.Pop();

                if (current is BoundMethodAccessExpression || current is BoundFunctionExpression)
                    continue;

                //Requires emittion
                if (current is BoundArrayAccessExpression arrayAccess)
                {
                    builder.Add(LineBreak());
                    useMacroAssignment = true;

                    var yield = nameYield.ToString();
                    nameYield.Clear();

                    builder.Add(Block(
                            GetArrayAccessAssignment(functionBuilder, yield, isSetter: false, arrayAccess, tempIndex),
                            GetAssignment(functionBuilder, symbol, _returnValue)
                        ));
                }
                else if (current is BoundCallExpression call)
                {
                    enforceStorage = true;
                    builder.Add(GetCallExpressionAssignment(functionBuilder, symbol, call, tempIndex));
                    nameYield.Clear();
                    nameYield.Append(_returnValue.Name);
                }
                else 
                    nameYield.Append(GetNameAddition(functionBuilder, current, ref enforceStorage));
            }

            var resultName = nameYield.ToString();

            if (useMacroAssignment)
                resultName = symbol.Name + resultName;

            var rightVar = functionBuilder.Scope.LookupOrDeclare(resultName, right.Type, false, false, enforceStorage ? DataLocation.Storage : null);

            if (rightVar != symbol)
                builder.Add(GetAssignment(functionBuilder, symbol, rightVar));
            
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private string GetNameAddition(MinecraftFunction.Builder functionBuilder, BoundExpression expression, ref bool enforceStorage)
        {
            if (expression is BoundVariableExpression variableExpression)
            {
                return ToEmittionVariable(functionBuilder, variableExpression.Variable, false, true).ScopedName;
            }
            else if (expression is BoundThisExpression thisExpression)
            {
                Debug.Assert(_thisSymbol != null);
                enforceStorage = true;
                return _thisSymbol.Name;
            }
            else if (expression is BoundNamespaceExpression namespaceExpression)
            {
                return namespaceExpression.Namespace.Name;
            }
            else if (expression is BoundFieldAccessExpression fa)
            {
                enforceStorage = true;
                return $".{fa.Field.Name}";
            }
            else
            {
                throw new Exception($"Unexpected expression kind {expression.Kind}");
            }
        }

        private Stack<BoundExpression> GetLeftAssociativeOrder(BoundExpression expression)
        {
            var leftAssociativeOrder = new Stack<BoundExpression>();
            leftAssociativeOrder.Push(expression);

            while (true)
            {
                var current = leftAssociativeOrder.Peek();

                if (current is BoundFieldAccessExpression fa)
                    leftAssociativeOrder.Push(fa.Instance);
                else if (current is BoundCallExpression call)
                    leftAssociativeOrder.Push(call.Identifier);
                else if (current is BoundMethodAccessExpression methodAccess)
                    leftAssociativeOrder.Push(methodAccess.Instance);
                else if (current is BoundArrayAccessExpression arrayAccess)
                    leftAssociativeOrder.Push(arrayAccess.Identifier);
                else
                    break;
            }

            return leftAssociativeOrder;
        }

        private TextEmittionNode GetAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression expression, int tempIndex = 0)
        {
            TextEmittionNode emittionNode;

            if (expression is BoundLiteralExpression l)
            {
                emittionNode = GetLiteralAssignment(functionBuilder, symbol, l);
            }
            else if (expression is BoundVariableExpression v)
            {
                emittionNode = GetVariableAssignment(functionBuilder, symbol, v);
            }
            else if (expression is BoundAssignmentExpression a)
            {
                return Block(
                        GetAssignment(functionBuilder, a.Left, a.Right, tempIndex),
                        GetAssignment(functionBuilder, symbol, a.Left, tempIndex)
                    );
            }
            else if (expression is BoundUnaryExpression u)
            {
                emittionNode = GetUnaryExpressionAssignment(functionBuilder, symbol, u, tempIndex);
            }
            else if (expression is BoundBinaryExpression b)
            {
                emittionNode = GetBinaryExpressionAssignment(functionBuilder, symbol, b, tempIndex);
            }
            else if (expression is BoundCallExpression c)
            {
                emittionNode = GetCallExpressionAssignment(functionBuilder, symbol, c, tempIndex);
            }
            else if (expression is BoundConversionExpression conv)
            {
                emittionNode = GetConversionExpressionAssignment(functionBuilder, symbol, conv, tempIndex);
            }
            else if (expression is BoundObjectCreationExpression objectCreation)
            {
                emittionNode = GetObjectCreationExpressionAssignment(functionBuilder, symbol, objectCreation);
            }
            else if (expression is BoundArrayCreationExpression arrayCreation)
            {
                emittionNode = GetArrayCreationExpressionAssignment(functionBuilder, symbol, arrayCreation, tempIndex);
            }
            else if (expression is BoundFieldAccessExpression fieldAccess)
            {
                emittionNode = GetFieldAccessAssignment(functionBuilder, symbol, fieldAccess, tempIndex);
            }
            else if (expression is BoundArrayAccessExpression arrayAccessExpression)
            {
                emittionNode = GetPrimaryAssignment(functionBuilder, symbol, arrayAccessExpression, tempIndex);
            }
            else
            {
                throw new Exception($"Unexpected expression kind {expression.Kind}");
            }
            return emittionNode;
        }

        private TextEmittionNode GetFieldAccessAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundFieldAccessExpression fieldAccess, int tempIndex = 0)
        {
            if (TryGetBuiltInFieldGetter(functionBuilder, symbol, fieldAccess, tempIndex, out var node))
            {
                Debug.Assert(node != null);
                return node;
            }

            return GetPrimaryAssignment(functionBuilder, symbol, fieldAccess, tempIndex);
        }

        private CommandNode GetAssignment(EmittionVariableSymbol symbol, object value, bool writeUnaltered = false)
        {
            if (symbol.Location == DataLocation.Storage)
            {
                if (writeUnaltered)
                    return DataSetValue(symbol, $"{value}");

                if (value is int)
                {
                    var stringValue = $"{value}";
                    return DataSetValue(symbol, stringValue);
                }
                else if (value is bool)
                {
                    var numberValue = (bool)value ? "1" : "0";
                    return DataSetValue(symbol, numberValue);
                }
                else if (value is string)
                {
                    var str = $"\"{((string)value).Replace("\"", "\\\"")}\"";
                    return DataSetValue(symbol, str);
                }
                else if (value is float)
                {
                    var stringValue = $"{value}f";
                    return DataSetValue(symbol, stringValue);
                }
                else if (value is double)
                {
                    var stringValue = $"{value}d";
                    return DataSetValue(symbol, stringValue);
                }
                else
                    throw new Exception($"Unexpected storage stored literal type {value.GetType()}");
            }
            else if (symbol.Location == DataLocation.Scoreboard)
            {
                if (value is bool)
                    return ScoreSet(symbol, (bool)value ? 1 : 0);
                else if (value is int)
                    return ScoreSet(symbol, (int)value);
                else
                    throw new Exception($"Unexpected scoreboard literal type {value.GetType()}");
            }
            else
                throw new Exception($"Unexpected emittion variable location {symbol.Location}");
        }

        private CommandNode GetGetterAssignment(EmittionVariableSymbol symbol, CommandNode getterCommand)
        {
            return symbol.Location switch
            {
                DataLocation.Scoreboard => Execute().StoreResultScore(symbol).Run(getterCommand),
                DataLocation.Storage => Execute().StoreResultStorage(symbol, EmittionFacts.TypeToStoreType(symbol.Type), "1").Run(getterCommand),
                _ => throw new Exception($"Unexpected symbol location {symbol.Location}")
            };
        }

        private TextEmittionNode GetLiteralAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundLiteralExpression literal) => GetAssignment(symbol, literal.Value, false);

        private TextEmittionNode GetVariableAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundVariableExpression variable)
        {
            if (variable.Variable is StringEnumMemberSymbol stringEnumMember)
                return GetAssignment(symbol, stringEnumMember.UnderlyingValue, false);
            else if (variable.Variable is IntEnumMemberSymbol intEnumMember)
                return GetAssignment(symbol, intEnumMember.UnderlyingValue, false);
            else
            {
                var rightVariable = ToEmittionVariable(functionBuilder, variable.Variable, false, !(variable.Variable is ParameterSymbol));
                return GetAssignment(functionBuilder, symbol, rightVariable);
            }
        }

        private CommandNode GetAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            if (left.Location == DataLocation.Storage && right.Location == DataLocation.Storage)
                return _factory.DataCopy(left, right);

            if (left.Location == DataLocation.Scoreboard && right.Location == DataLocation.Scoreboard)
                return _factory.ScoreCopy(left, right);

            if (left.Location == DataLocation.Storage && right.Location == DataLocation.Scoreboard)
                return Execute().StoreResultStorage(left, StoreType.Int, "1").Run(ScoreGet(right));

            if (left.Location == DataLocation.Scoreboard && right.Location == DataLocation.Storage)
                return Execute().StoreResultScore(left).Run(_factory.StorageGet(right));

            throw new Exception($"Unexpected variable location combination {left.Location} and {right.Location}");
        }

        private CommandNode IfValueMatchesRun(EmittionVariableSymbol variable, string value, CommandNode resultCommand)
        {
            return variable.Location switch
            {
                DataLocation.Scoreboard => Execute().IfScoreMatches(variable, value).Run(resultCommand),
                DataLocation.Storage => Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{variable.SaveName}\": {value}}}").Run(resultCommand),
                _ => throw new Exception($"Unexpected variable location {variable.Location}")
            };
        }

        private TextEmittionNode GetMacroAssignment(MinecraftFunction.Builder functionBuilder, MacroEmittionVariableSymbol left, BoundExpression right, int tempIndex)
        {
            var rightVar = new EmittionVariableSymbol("*right", right.Type, true, null, null);
            functionBuilder.Scope.Declare(rightVar);

            MinecraftFunction.Builder assignmentMacro;

            if (rightVar.Location == DataLocation.Storage)
            {
                if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.AssignStSt, out assignmentMacro))
                    assignmentMacro.AddMacro(_factory.StorageCopy(left.Accessor, rightVar.SaveName));
            }
            else
            {
                if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.AssignStSc, out assignmentMacro))
                    assignmentMacro.AddMacro(Execute()
                                            .StorePath(YieldType.Result, _factory.ToStoragePathIdentifier(left.Accessor), StoreType.Int, "1")
                                            .Run(ScoreGet(rightVar)));
            }

            return Block(
                    GetResetCommand(rightVar),
                    GetAssignment(functionBuilder, rightVar, right, tempIndex),
                    GetMacroCall(assignmentMacro)
                );
        }

        private bool GetFabricatedFunction(string name, TypeSymbol type, out MinecraftFunction.Builder builder)
        {
            var fabricated = _fabricatedMacroFunctions.FirstOrDefault(f => f.Name == name);

            if (fabricated == null)
            {
                fabricated = new FunctionSymbol(name, BuiltInNamespace.Blaze.Fabricated.Symbol, ImmutableArray<ParameterSymbol>.Empty, type, false, false, AccessModifier.Private, null);
                BuiltInNamespace.Blaze.Fabricated.Symbol.Members.Add(fabricated);
                _fabricatedMacroFunctions.Add(fabricated);
            }

            return GetOrCreateBuiltIn(fabricated, out builder);
        }

        private TextEmittionNode GetObjectCreationExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundObjectCreationExpression objectCreationExpression)
        {
            var constructor = objectCreationExpression.NamedType.Constructor;
            Debug.Assert(constructor != null);
            Debug.Assert(constructor.FunctionBody != null);

            var previousContextSymbol = _thisSymbol;
            _thisSymbol = symbol;
            
            var node = Block(
                    GetArgumentAssignment(functionBuilder, constructor, constructor.Parameters, objectCreationExpression.Arguments),
                    Comment($"Emitting object creation of type {objectCreationExpression.NamedType.Name}, stored in variable \"{symbol.SaveName}\""),
                    GetStatement(functionBuilder, constructor.FunctionBody)
                ); 

            _thisSymbol = previousContextSymbol;
            return node;
        }

        private TextEmittionNode GetArrayCreationExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundArrayCreationExpression arrayCreationExpression, int tempIndex)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            EmittionVariableSymbol? previous = null;
            
            builder.Add(Comment($"Emitting array creation of type {arrayCreationExpression.ArrayType}, stored in variable \"{symbol.SaveName}\""));

            for (int i = arrayCreationExpression.Dimensions.Length - 1; i >= 0; i--)
            {
                EmittionVariableSymbol arraySymbol;

                if (i == 0)
                    arraySymbol = symbol;
                else
                    arraySymbol = new EmittionVariableSymbol($"*rank{tempIndex + i}", TypeSymbol.Object, true, null, null);
                
                TextEmittionNode assignmentCommand;

                if (previous == null)
                {
                    var defaultValue = EmittionFacts.GetEmittionDefaultValue(arrayCreationExpression.ArrayType.Type);

                    if (arrayCreationExpression.Dimensions[i] is BoundLiteralExpression literal)
                    {
                        var initializerBuilder = new StringBuilder();
                        var dimension = (int)literal.Value;

                        initializerBuilder.Append("[");

                        for (int j = 0; j < dimension; j++)
                        {
                            initializerBuilder.Append($"{defaultValue}");

                            if (j != dimension - 1)
                                initializerBuilder.Append($", ");
                        }

                        initializerBuilder.Append("]");
                        assignmentCommand = GetAssignment(arraySymbol, initializerBuilder.ToString(), true);
                        builder.Add(assignmentCommand);
                        previous = arraySymbol;
                        continue;
                    }
                    else
                        assignmentCommand = _factory.DataModifyValue(arraySymbol, DataModifyCommand.ModificationType.Append, defaultValue);
                }
                else
                    assignmentCommand = _factory.DataModifyFrom(arraySymbol, DataModifyCommand.ModificationType.Append, previous);
                
                builder.Add(GetAssignment(arraySymbol, "[]", true));

                var subFunction = functionBuilder.CreateSub(SubFunctionKind.Loop);
                var callCommand = GetCall(subFunction);
                
                var tempIter = Temp(functionBuilder, TypeSymbol.Int, tempIndex + i, "iter");
                var tempUpperBound = Temp(functionBuilder, TypeSymbol.Int, tempIndex + i, "upperBound");

                builder.Add(GetAssignment(functionBuilder, tempIter, new BoundLiteralExpression(0), tempIndex + i));
                builder.Add(GetAssignment(functionBuilder, tempUpperBound, arrayCreationExpression.Dimensions[i], tempIndex + i));
                builder.Add(callCommand);

                if (previous != null)
                    builder.Add(GetResetCommand(previous));

                subFunction.Content.Add(assignmentCommand);
                subFunction.Content.Add(_factory.ScoreAdd(tempIter, 1));
                subFunction.Content.Add(Execute()
                                        .IfScore(tempIter, BoundBinaryOperatorKind.Less, tempUpperBound)
                                        .Run(callCommand));

                previous = arraySymbol;
            }
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode GetUnaryExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundUnaryExpression unary, int tempIndex)
        {
            var operand = unary.Operand;
            var operatorKind = unary.Operator.OperatorKind;

            switch (operatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return Block(
                            Comment($"Emitting unary identity expression \"{unary}\" to \"{symbol.SaveName}\""),
                            GetAssignment(functionBuilder, symbol, operand, tempIndex)
                        );

                case BoundUnaryOperatorKind.Negation:
                    if (operand.Type == TypeSymbol.Int)
                        return GetIntNegation(functionBuilder, symbol, unary, tempIndex, operand);        
                    else
                        return GetFloatingPointNegation(functionBuilder, symbol, unary, tempIndex, operand);
            
                case BoundUnaryOperatorKind.LogicalNegation:
                    return GetLogicalNegation(functionBuilder, symbol, unary, tempIndex, operand);

                default:
                    throw new Exception($"Unexpected unary operator kind {operatorKind}");
            }
        }

        private TextEmittionNode GetIntNegation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundUnaryExpression unary, int tempIndex, BoundExpression operand)
        {
            var operandSymbol = symbol.Location == DataLocation.Storage
                                ? Temp(functionBuilder, operand.Type, tempIndex)
                                : symbol;

            var minusOneConst = new EmittionVariableSymbol("-1", TypeSymbol.Int, false);

            return Block(
                    LineBreak(),
                    Comment($"Emitting integer negation unary expression \"{unary}\" to \"{symbol.SaveName}\""),
                    GetAssignment(functionBuilder, operandSymbol, operand, tempIndex),

                    symbol.Location == DataLocation.Scoreboard
                        ? _factory.ScoreOperation(symbol, BoundBinaryOperatorKind.Multiplication, minusOneConst)
                        : Execute().StoreResultStorage(symbol, StoreType.Int, "-1").Run(ScoreGet(operandSymbol))
                );
        }

        private TextEmittionNode GetFloatingPointNegation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundUnaryExpression unary, int tempIndex, BoundExpression operand)
        {
            var macroA = Macro(functionBuilder, "a");
            var macroSign = Macro(functionBuilder, "sign");

            var sign = Temp(functionBuilder, TypeSymbol.Object, tempIndex, "sign");
            var last = Temp(functionBuilder, TypeSymbol.Object, tempIndex, "last");

            MinecraftFunction.Builder macro;

            if (operand.Type == TypeSymbol.Float)
            {
                if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateFloat, out macro))
                    macro.AddMacro(DataSetValue(_returnValue, $"{macroSign.Accessor}{macroA.Accessor}f"));
            }
            else
            {
                if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateDouble, out macro))
                    macro.AddMacro(DataSetValue(_returnValue, $"{macroSign.Accessor}{macroA.Accessor}d"));
            }

            var typeSuffix = operand.Type == TypeSymbol.Float ? "f" : "d";

            return Block(
                    LineBreak(),
                    Comment($"Emitting floating point negation unary expression \"{unary}\" to \"{symbol.SaveName}\""),
                    GetAssignment(functionBuilder, symbol, operand, tempIndex),
                    GetAssignment(functionBuilder, macroA, symbol),
                    DataStringCopy(sign, symbol, 0, 1),
                    DataStringCopy(last, symbol, -1),
                    Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{sign.SaveName}\": \"-\"}}").Run(DataStringCopy(macroA, macroA, 1)),
                    Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{last.SaveName}\": \"{typeSuffix}\"}}").Run(DataStringCopy(macroA, macroA, 0, -1)),
                    Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{sign.SaveName}\": \"-\"}}").Run(DataSetValue(macroSign, "\"\"")),
                    Execute().UnlessData(DataLocation.Storage, MainStorage, $"{{ \"{sign.SaveName}\": \"-\"}}").Run(DataSetValue(macroSign, "\"-\"")),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, symbol, _returnValue)
                );
        }

        private TextEmittionNode GetLogicalNegation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundUnaryExpression unary, int tempIndex, BoundExpression operand)
        {
            var temp = Temp(functionBuilder, operand.Type, tempIndex);

            if (symbol.Location == DataLocation.Scoreboard)
            {
                return Block(
                    LineBreak(),
                    Comment($"Emitting logical negation \"{unary}\" to \"{symbol.SaveName}\""),
                    GetAssignment(functionBuilder, temp, operand, tempIndex),
                    Execute().IfScoreMatches(temp, "1").Run(ScoreSet(symbol, 0)),
                    Execute().IfScoreMatches(temp, "0").Run(ScoreSet(symbol, 1))
                );
            }
            else
            {
                return Block(
                    LineBreak(),
                    Comment($"Emitting logical negation \"{unary}\" to \"{symbol.SaveName}\""),
                    GetAssignment(functionBuilder, temp, operand, tempIndex),
                    Execute().IfScoreMatches(temp, "1").Run(DataSetValue(symbol, "0")),
                    Execute().IfScoreMatches(temp, "0").Run(DataSetValue(symbol, "1"))
                );
            }
        }

        private TextEmittionNode GetBinaryExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundBinaryExpression binary, int tempIndex)
        {
            var left = binary.Left;
            var right = binary.Right;
            var operatorKind = binary.Operator.OperatorKind;

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Addition:
                case BoundBinaryOperatorKind.Subtraction:
                case BoundBinaryOperatorKind.Multiplication:
                case BoundBinaryOperatorKind.Division:
                    if (left.Type == TypeSymbol.Int)
                    {
                        return Block(
                                LineBreak(),
                                Comment($"Emitting binary expression \"{binary}\" to \"{symbol.SaveName}\""),
                                GetIntBinaryAssignment(functionBuilder, symbol, left, right, operatorKind, tempIndex)
                            );
                    }
                    else if (left.Type == TypeSymbol.Float || left.Type == TypeSymbol.Double)
                    {
                        return Block(
                                LineBreak(),
                                Comment($"Emitting floating point binary operation \"{binary}\" to \"{symbol.SaveName}\""),
                                GetFloatingPointBinaryAssignment(functionBuilder, symbol, left, right, operatorKind, tempIndex)
                            );
                    }
                    else if (left.Type == TypeSymbol.String && right.Type == TypeSymbol.String)
                    {
                        if (operatorKind == BoundBinaryOperatorKind.Addition)
                            return Block(
                                    GetAssignment(functionBuilder, symbol, left),
                                    GetStringConcatenation(functionBuilder, symbol, right, tempIndex)
                                ); 
                    }
                    throw new Exception($"Unexpected operator kind {operatorKind} for types {left.Type} and {right.Type}");

                case BoundBinaryOperatorKind.LogicalMultiplication:
                    {
                        var leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, $"lbTemp");
                        var rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, $"rbTemp");

                        return Block(
                                LineBreak(),
                                Comment($"Emitting logical multiplication (and) operation \"{binary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                GetAssignment(symbol, 0),
                                Execute().IfScoreMatches(leftSymbol, "1").IfScoreMatches(rightSymbol, "1").Run(GetAssignment(symbol, 1))
                            );
                    }
                case BoundBinaryOperatorKind.LogicalAddition:
                    {
                        var leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, $"lbTemp");
                        var rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, $"rbTemp");
                   
                        return Block(
                                LineBreak(),
                                Comment($"Emitting logical multiplication (and) operation \"{binary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                GetAssignment(symbol, 0),
                                IfValueMatchesRun(leftSymbol, "1", GetAssignment(symbol, 1)),
                                IfValueMatchesRun(rightSymbol, "1", GetAssignment(symbol, 1))
                            );
                    }
                case BoundBinaryOperatorKind.Equals:
                    {
                        if (left.Type == TypeSymbol.Float || left.Type == TypeSymbol.Double)
                            return EmitFloatingPointComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                        else if (left.Type == TypeSymbol.Int && right.Type == TypeSymbol.Int || left.Type is EnumSymbol e && e.IsIntEnum)
                            return EmitIntComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                        else
                        {
                            var leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, $"lbTemp");
                            var rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, $"rbTemp");
                            var resultTemp = Temp(functionBuilder, left.Type, tempIndex + 1);

                            return Block(
                                    LineBreak(),
                                    Comment($"Emitting equals operation \"{binary}\" to \"{symbol.SaveName}\""),
                                    GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                    GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                    GetGetterAssignment(resultTemp, GetAssignment(functionBuilder, leftSymbol, rightSymbol)),

                                    IfValueMatchesRun(resultTemp, "1", GetAssignment(symbol, 0)),
                                    IfValueMatchesRun(resultTemp, "0", GetAssignment(symbol, 1))
                                );
                        }
                    }
                case BoundBinaryOperatorKind.NotEquals:
                    {
                        if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object || left.Type is EnumSymbol e && !e.IsIntEnum)
                        {
                            var leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, $"lbTemp");
                            var rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, $"rbTemp");
                            
                            return Block(
                                    LineBreak(),
                                    Comment($"Emitting equals operation \"{binary}\" to \"{symbol.SaveName}\""),
                                    GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                    GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                    GetGetterAssignment(symbol, GetAssignment(functionBuilder, leftSymbol, rightSymbol))
                                );
                        }
                        else
                            return EmitIntComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                    }
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.LessOrEquals:
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return 
                        left.Type == TypeSymbol.Int
                            ? EmitIntComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex)
                            : EmitFloatingPointComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);

                default:
                    throw new Exception($"Unexpected binary operator kind {operatorKind} for types {left.Type} and {right.Type}");
            }
        }

        private TextEmittionNode GetStringConcatenation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression right, int tempIndex)
        {
            var macroLeft = Macro(functionBuilder, "str_left");
            var macroRight = Macro(functionBuilder, "right");

            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.StrConcat, out var macro))
                macro.AddMacro(DataSetValue(_returnValue, $"\"{macroLeft.Accessor}{macroRight.Accessor}\""));

            return Block(
                    GetAssignment(functionBuilder, macroLeft, symbol),
                    GetAssignment(functionBuilder, macroRight, right, tempIndex),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, symbol, _returnValue)
                );
        }

        private TextEmittionNode GetIntBinaryAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int tempIndex)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            var leftSymbol = symbol.Location == DataLocation.Storage
                    ? Temp(functionBuilder, TypeSymbol.Int, tempIndex + 1)
                    : symbol;

            EmittionVariableSymbol? rightSymbol = null;
            builder.Add(GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1));

            if (right is BoundLiteralExpression l)
            {
                if (operation == BoundBinaryOperatorKind.Addition)
                {
                    builder.Add(_factory.ScoreAdd(leftSymbol, (int) l.Value));

                    if (symbol.Location == DataLocation.Storage)
                        builder.Add(GetAssignment(functionBuilder, symbol, leftSymbol));

                    return new TextBlockEmittionNode(builder.ToImmutable());
                }
                else if (operation == BoundBinaryOperatorKind.Subtraction)
                {
                    builder.Add(_factory.ScoreSubtract(leftSymbol, (int) l.Value));

                    if (symbol.Location == DataLocation.Storage)
                        builder.Add(GetAssignment(functionBuilder, symbol, leftSymbol));

                    return new TextBlockEmittionNode(builder.ToImmutable());
                }
                else
                {
                    rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, "rTemp");
                    builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1));
                }
            }
            else if (right is BoundVariableExpression v)
            {
                var rightExpressionVar = ToEmittionVariable(functionBuilder, v.Variable, false, true);

                if (rightExpressionVar.Location == DataLocation.Scoreboard)
                    rightSymbol = rightExpressionVar;
            }

            if (rightSymbol == null)
            {
                rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, "rTemp");
                builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1));
            }

            builder.Add(_factory.ScoreOperation(leftSymbol, operation, rightSymbol));

            if (symbol.Location == DataLocation.Storage)
                builder.Add(GetAssignment(functionBuilder, symbol, leftSymbol));

            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode EmitIntComparisonOperation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int tempIndex)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            EmittionVariableSymbol? leftSymbol = null;

            var initialValue = operatorKind == BoundBinaryOperatorKind.NotEquals ? 1 : 0;
            var successValue = operatorKind == BoundBinaryOperatorKind.NotEquals ? 0 : 1;

            if (left is BoundVariableExpression v)
            {
                //TODO: remove this when constant folding will be in place
                //This can only occur when two constants are compared

                if (v.Variable is IntEnumMemberSymbol enumMember)
                {
                    var other = (IntEnumMemberSymbol) ((BoundVariableExpression)right).Variable;
                    var result = (other.UnderlyingValue == enumMember.UnderlyingValue) ? successValue : initialValue;
                    return ScoreSet(symbol, result);
                }

                var leftExpressionSymbol = ToEmittionVariable(functionBuilder, v.Variable, false, true);

                if (leftExpressionSymbol.Location == DataLocation.Scoreboard)
                    leftSymbol = leftExpressionSymbol;
            }

            if (leftSymbol == null)
            {
                leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, "lTemp", DataLocation.Scoreboard);
                builder.Add(GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1));
            }

            builder.Add(GetAssignment(symbol, initialValue));

            if (right is BoundLiteralExpression l && l.Value is int value)
            {
                int? lowerBound = operatorKind switch
                {
                    BoundBinaryOperatorKind.Greater => value + 1,
                    BoundBinaryOperatorKind.GreaterOrEquals => value,
                    BoundBinaryOperatorKind.Equals => value,
                    BoundBinaryOperatorKind.NotEquals => value,
                    _ => null
                };

                int? upperBound = operatorKind switch
                {
                    BoundBinaryOperatorKind.Less => value - 1,
                    BoundBinaryOperatorKind.LessOrEquals => value,
                    BoundBinaryOperatorKind.Equals => value,
                    BoundBinaryOperatorKind.NotEquals => value,
                    _ => null
                };

                builder.Add(Execute().IfScoreMatches(leftSymbol, lowerBound?.ToString(), upperBound?.ToString()).Run(GetAssignment(symbol, successValue)));
            }
            else
            {
                EmittionVariableSymbol? rightSymbol = null;

                if (right is BoundVariableExpression vr)
                {
                    if (vr.Variable is IntEnumMemberSymbol enumMember)
                    {
                        var memberUnderlyingValue = enumMember.UnderlyingValue.ToString();
                        builder.Add(Execute().IfScoreMatches(leftSymbol, memberUnderlyingValue).Run(GetAssignment(symbol, successValue)));
                        return new TextBlockEmittionNode(builder.ToImmutable());
                    }

                    var rightExpressionVariable = ToEmittionVariable(functionBuilder, vr.Variable, false, true);

                    if (rightExpressionVariable.Location == DataLocation.Scoreboard)
                        rightSymbol = rightExpressionVariable;
                }

                if (rightSymbol == null)
                {
                    rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, "rTemp", DataLocation.Scoreboard);
                    builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1));
                }

                if (operatorKind == BoundBinaryOperatorKind.NotEquals)
                    operatorKind = BoundBinaryOperatorKind.Equals;

                builder.Add(Execute().IfScore(leftSymbol, operatorKind, rightSymbol).Run(GetAssignment(symbol, successValue)));
            }

            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode EmitFloatingPointComparisonOperation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int tempIndex)
        {
            var macroY = Macro(functionBuilder, "y");
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.PositionY, out var macro))
                macro.AddMacro(TeleportCommand.ToLocation("@s", _debugChunk.X, macroY.Accessor, _debugChunk.Z));
            
            builder.Add(GetAssignment(functionBuilder, macroY, left, tempIndex));
            builder.Add(Execute().As(_mathEntity1).Run(GetMacroCall(macro)));

            builder.Add(GetAssignment(functionBuilder, macroY, right, tempIndex));
            builder.Add(Execute().As(_mathEntity2).Run(GetMacroCall(macro)));

            builder.Add(GetAssignment(symbol, operatorKind == BoundBinaryOperatorKind.NotEquals ? 1 : 0));
            builder.Add(operatorKind switch
            {
                BoundBinaryOperatorKind.Equals =>
                        Execute().As(_mathEntity1).At("@s")
                                 .IfEntity("@e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 1)),
                    
                BoundBinaryOperatorKind.NotEquals =>
                        Execute().As(_mathEntity1).At("@s")
                                 .IfEntity("@e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 0)),

                BoundBinaryOperatorKind.Greater =>
                        Execute().Positioned(_debugChunk.X, "19999999.9999", _debugChunk.Z)
                                 .As("@e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1]")
                                 .IfEntity("@s[tag=first]").At("@s")
                                 .UnlessEntity("@e[type=item_display,tag=blz,tag=debug,tag=second,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 1)),

                BoundBinaryOperatorKind.GreaterOrEquals => Block(
                        Execute().Positioned(_debugChunk.X, "19999999.9999", _debugChunk.Z)
                                 .As("@e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1]")
                                 .IfEntity("@s[tag=first]")
                                 .Run(GetAssignment(symbol, 1)),
                        Execute().As(_mathEntity1).At("@s")
                                 .IfEntity("@e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 1))
                    ),

                BoundBinaryOperatorKind.Less =>
                        Execute().Positioned(_debugChunk.X, "19999999.9999", _debugChunk.Z)
                                 .As("@e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1]")
                                 .IfEntity("@s[tag=second]").At("@s")
                                 .UnlessEntity("@e[type=item_display,tag=blz,tag=debug,tag=first,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 1)),

                BoundBinaryOperatorKind.LessOrEquals => Block(
                        Execute().Positioned(_debugChunk.X, "19999999.9999", _debugChunk.Z)
                                 .As("@e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1]")
                                 .IfEntity("@s[tag=second]")
                                 .UnlessEntity("@e[type=item_display,tag=blz,tag=debug,tag=first,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 1)),
                        Execute().As(_mathEntity1).At("@s")
                                 .IfEntity("@e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.000001]")
                                 .Run(GetAssignment(symbol, 1))
                    ),

                _ => throw new Exception($"Unexpected binary operator kind {operatorKind}")
            });
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode GetFloatingPointBinaryAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind kind, int current)
        {
            var blockBuilder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var leftAssignment = GetAssignment(functionBuilder, symbol, left, current);
            blockBuilder.Add(leftAssignment);

            var rightSymbol = Temp(functionBuilder, right.Type, current + 1, "rTemp");
            var rightAssignment = GetAssignment(functionBuilder, rightSymbol, right, current + 1);
            blockBuilder.Add(rightAssignment);
            
            var macroA = Macro(functionBuilder, "a");
            var macroB = Macro(functionBuilder, "b");
            var macroPolarity = Macro(functionBuilder, "polarity");

            if (kind != BoundBinaryOperatorKind.Subtraction)
            {
                blockBuilder.Add(GetAssignment(functionBuilder, macroA, symbol));
                blockBuilder.Add(GetAssignment(functionBuilder, macroB, rightSymbol));
            } 

            MinecraftFunction.Builder macro;
            var entity = _mathEntity1.ToString();

            switch (kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    {
                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Add, out macro))
                            macro.AddMacro(Execute().Positioned("~", macroA.Accessor, "~")
                                                    .Run(new TeleportToLocationCommand(entity, _debugChunk.WithY($"~{macroB.Accessor}"))));
                        
                        break;
                    }
                case BoundBinaryOperatorKind.Subtraction:
                    {
                        var last = functionBuilder.Scope.LookupOrDeclare("*last", TypeSymbol.Object, true, false);
                        var pol = functionBuilder.Scope.LookupOrDeclare("*pol", TypeSymbol.Object, true, false);

                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Subtract, out macro))
                            macro.AddMacro(Execute().Positioned("~", macroA.Accessor, "~")
                                                    .Run(new TeleportToLocationCommand(entity, _debugChunk.WithY($"~{macroPolarity.Accessor}{macroB.Accessor}"))));

                        if (macro.GetOrCreateSub("if_minus", out var sub))
                        {
                            sub.Content.Add(DataStringCopy(macroA, macroA, 1));
                            sub.Content.Add(DataStringCopy(last, macroA, -1));
                            sub.Content.Add(Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{last.SaveName}\" : \"d\" }}")
                                                     .Run(DataStringCopy(macroA, macroA, 0, -1)));
                            sub.Content.Add(Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{last.SaveName}\" : \"f\" }}")
                                                     .Run(DataStringCopy(macroA, macroA, 0, -1)));
                            sub.Content.Add(GetAssignment(macroPolarity, string.Empty));
                        }

                        blockBuilder.Add(GetAssignment(functionBuilder, macroB, symbol));
                        blockBuilder.Add(GetAssignment(functionBuilder, macroA, rightSymbol));
                        blockBuilder.Add(GetAssignment(functionBuilder, pol, rightSymbol));
                        blockBuilder.Add(DataStringCopy(pol, pol, 0, 1));
                        blockBuilder.Add(Execute().IfData(DataLocation.Storage, MainStorage, $"{{ \"{pol.SaveName}\" : \"-\" }}").Run(GetCall(sub)));
                        blockBuilder.Add(Execute().UnlessData(DataLocation.Storage, MainStorage, $"{{ \"{pol.SaveName}\" : \"-\" }}").Run(DataSetValue(macroPolarity, "\"-\"")));

                        blockBuilder.Add(GetDoubleConversion(functionBuilder, macroA, macroA));
                        break;
                    }
                case BoundBinaryOperatorKind.Multiplication:
                    {
                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Multiply, out macro))
                        {
                            macro.AddMacro(DataSetValue(_returnValue, $"[0f, 0f, 0f,{macroA.Accessor}f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f]"));
                            macro.AddMacro(new TextCommand($"data modify entity {entity} transformation set value [0f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,{macroB.Accessor}f]"));
                        }
                        break;
                    }
                case BoundBinaryOperatorKind.Division:
                    {
                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Divide, out macro))
                            macro.AddMacro(new TextCommand($"data modify entity {entity} transformation set value [0f, 0f, 0f,{macroA.Accessor}f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,{macroB.Accessor}f]"));
                        break;
                    }

                default:
                    throw new Exception($"Unexpected binary operation kind {kind}");
            }

            blockBuilder.Add(GetMacroCall(macro));

            if (kind == BoundBinaryOperatorKind.Addition || kind == BoundBinaryOperatorKind.Subtraction)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set from entity {_mathEntity1.ToString()} Pos[1]"));
                blockBuilder.Add(TeleportCommand.ToLocation(entity, _debugChunk.X, "0", _debugChunk.Z));

                if (left.Type == TypeSymbol.Float)
                    blockBuilder.Add(GetFloatConversion(functionBuilder, symbol, symbol));
            }
            else if (kind == BoundBinaryOperatorKind.Multiplication)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {_returnValue.SaveName}[-1] set from entity {entity} transformation.translation[0]"));
                blockBuilder.Add(new TextCommand($"data modify entity {entity} transformation set from storage {MainStorage} {_returnValue.SaveName}"));
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set from entity {entity} transformation.translation[0]"));
                functionBuilder.Scope.Declare(_returnValue);

                if (left.Type == TypeSymbol.Double)
                    blockBuilder.Add(GetDoubleConversion(functionBuilder, symbol, symbol));
            }
            else if (kind == BoundBinaryOperatorKind.Division)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set from entity {entity} transformation.translation[0]"));

                if (left.Type == TypeSymbol.Double)
                    blockBuilder.Add(GetDoubleConversion(functionBuilder, symbol, symbol));
            }

            return new TextBlockEmittionNode(blockBuilder.ToImmutable());
        }

        private TextEmittionNode GetCallExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundCallExpression call, int current)
        {
            if (TryGetBuiltInFunctionEmittion(functionBuilder, symbol, call, current, out var node))
            {
                Debug.Assert(node != null);
                return node;
            }

            return Block(
                    Comment($"Assigning return value of {call.Function.Name} to \"{symbol.SaveName}\""),
                    GetCallExpression(functionBuilder, symbol, call, current),
                    GetAssignment(functionBuilder, symbol, _returnValue)
                );
        }

        private TextEmittionNode GetArgumentAssignment(MinecraftFunction.Builder functionBuilder, FunctionSymbol calledFunction, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<BoundExpression> arguments)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var parameter = parameters[i];

                var parameterVar = ToEmittionVariable(functionBuilder, parameter, true, false);
                var assignment = GetAssignment(functionBuilder, parameterVar, argument, 0);

                builder.Add(assignment);
            }

            if (builder.Count == 1)
                return builder.First();
            else
                return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode GetConversionExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundConversionExpression conversion, int current)
        {
            var resultType = conversion.Type;
            var sourceType = conversion.Expression.Type;

            if (sourceType is NamedTypeSymbol && resultType is NamedTypeSymbol)
            {
                return Block(
                        Comment($"Assigning a conversion from {conversion.Expression.Type} to {conversion.Type} to variable \"{symbol.SaveName}\""),
                        GetAssignment(functionBuilder, symbol, conversion.Expression, current)
                    );
            }
            else
            {
                var temp = Temp(functionBuilder, conversion.Expression.Type, current);
                
                if (resultType == TypeSymbol.String)
                {
                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        var temp2 = Temp(functionBuilder, TypeSymbol.String, current + 1);

                        return Block(
                                GetAssignment(functionBuilder, temp, conversion.Expression, current),
                                GetAssignment(functionBuilder, temp2, temp),
                                DataStringCopy(symbol, temp2)
                            );
                    }
                    else if (sourceType is EnumSymbol enumSymbol)
                    {
                        var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
                        builder.Add(GetAssignment(functionBuilder, temp, conversion.Expression, current));

                        if (enumSymbol.IsIntEnum)
                        {
                            foreach (var enumMember in enumSymbol.Members)
                            {
                                var intMember = (IntEnumMemberSymbol) enumMember;
                                var command = Execute().IfScoreMatches(temp, intMember.UnderlyingValue.ToString()).Run(DataSetValue(symbol, $"\"{enumMember.Name}\""));
                                builder.Add(command);
                            }
                            return new TextBlockEmittionNode(builder.ToImmutable());
                        }
                        else
                        {
                            return Block(
                                    GetAssignment(functionBuilder, temp, conversion.Expression, current),
                                    GetAssignment(functionBuilder, symbol, temp)
                                );
                        }
                    }
                    else if (sourceType == TypeSymbol.Float || sourceType == TypeSymbol.Double || sourceType == TypeSymbol.Object)
                    {
                        return Block(
                            GetAssignment(functionBuilder, temp, conversion.Expression, current),
                            DataStringCopy(symbol, temp)
                        );
                    }
                    else
                    {
                        throw new Exception($"Unexpected source type for string conversion: {sourceType}");
                    }
                }
                if (resultType == TypeSymbol.Float)
                {
                    return Block(
                            GetAssignment(functionBuilder, temp, conversion.Expression, current),
                            GetFloatConversion(functionBuilder, symbol, temp)
                        );
                }
                if (resultType == TypeSymbol.Double)
                {
                    return Block(
                            GetAssignment(functionBuilder, temp, conversion.Expression, current),
                            GetDoubleConversion(functionBuilder, symbol, temp)
                        );
                }
                else
                {
                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        return Block(
                            GetAssignment(functionBuilder, temp, conversion.Expression, current),
                            GetAssignment(functionBuilder, symbol, temp)
                        );
                    }
                    else
                    {
                        return Block(
                            GetAssignment(functionBuilder, temp, conversion.Expression, current),
                            GetAssignment(functionBuilder, symbol, temp)
                        );
                    }
                }
            }
        }
        private TextEmittionNode GetFloatConversion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            var macroA = Macro(functionBuilder, "a");

            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToFloat, out var macro))
                macro.AddMacro(DataSetValue(_returnValue, $"{macroA.Accessor}f"));

            return Block(
                    GetAssignment(functionBuilder, macroA, right),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, left, _returnValue)
                );
        }

        private TextEmittionNode GetDoubleConversion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            var macroA = Macro(functionBuilder, "a");

            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToDouble, out var macro))
                macro.AddMacro(DataSetValue(_returnValue, $"{macroA.Accessor}d"));

            return Block(
                    GetAssignment(functionBuilder, macroA, right),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, left, _returnValue)
                );
        }

        private EmittionVariableSymbol Temp(MinecraftFunction.Builder functionBuilder, TypeSymbol type, int tempIndex, string tempName = "temp", DataLocation? location = null)
        {
            var name = $"*{tempName}{tempIndex}";
            return functionBuilder.Scope.LookupOrDeclare(name, type, true, false, location);
        }

        private MacroEmittionVariableSymbol Macro(MinecraftFunction.Builder functionBuilder, string subName)
        {
            functionBuilder.Scope.Declare(_macro);
            return new MacroEmittionVariableSymbol(subName);
        }

        private bool GetOrCreateBuiltIn(FunctionSymbol function, out MinecraftFunction.Builder builder)
        {
            if (!_usedBuiltIn.ContainsKey(function))
            {
                builder = new MinecraftFunction.Builder(function.Name, _configuration.RootNamespace, null, function, null);
                _usedBuiltIn.Add(function, builder);
                return true;
            }
            else
            {
                builder = _usedBuiltIn[function];
                return false;
            }
        }

        public bool TryGetBuiltInFieldGetter(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundFieldAccessExpression right, int current, out TextEmittionNode? node)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(right.Field))
            {
                node = GetGetterAssignment(symbol, new GameruleCommand(right.Field.Name, null));
                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == right.Field)
            {
                node = GetGetterAssignment(symbol, new DifficultyCommand(null));
                return true;
            }
            node = null;
            return false;
        }

        public bool TrygetBuiltInFieldAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int current, out TextEmittionNode? node)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(field))
            {
                node = GetGameruleAssignment(functionBuilder, field, right, current);
                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == field)
            {
                node = GetDifficultyAssignment(functionBuilder, field, right, current);
                return true;
            }
            node = null;
            return false;
        }

        private TextEmittionNode GetGameruleAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol gameruleField, BoundExpression right, int tempIndex)
        {
            var temp = Temp(functionBuilder, right.Type, tempIndex);

            if (gameruleField.Type == TypeSymbol.Bool)
            {
                return Block(
                        GetAssignment(functionBuilder, temp, right, tempIndex),
                        Execute().IfScoreMatches(temp, "1").Run(new GameruleCommand(gameruleField.Name, "true")),
                        Execute().IfScoreMatches(temp, "0").Run(new GameruleCommand(gameruleField.Name, "false"))
                    );
            }
            else
            {
                var macroRule = Macro(functionBuilder, "rule");
                var macroValue = Macro(functionBuilder, "value");

                var macroFunctionSymbol = BuiltInNamespace.Minecraft.General.Gamerules.SetGamerule;

                if (GetOrCreateBuiltIn(macroFunctionSymbol, out var macro))
                    macro.AddMacro(new GameruleCommand(macroRule.Accessor, macroValue.Accessor));

                return Block(
                        GetAssignment(functionBuilder, temp, right, tempIndex),
                        GetAssignment(macroRule, $"{gameruleField.Name}"),
                        GetAssignment(functionBuilder, macroValue, temp),
                        GetMacroCall(macro)
                    );
            }
        }

        private TextEmittionNode GetDifficultyAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int tempIndex)
        {
            if (right is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                return new DifficultyCommand(em.Name.ToLower());

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var rightSymbol = Temp(functionBuilder, right.Type, tempIndex);
            builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex));

            foreach (var enumMember in BuiltInNamespace.Minecraft.General.Difficulty.Members)
            {
                var intMember = (IntEnumMemberSymbol)enumMember;
                builder.Add(Execute().IfScoreMatches(rightSymbol, intMember.UnderlyingValue.ToString())
                                     .Run(new DifficultyCommand(enumMember.Name.ToLower())));
            }
            
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        public bool TryGetBuiltInFunctionEmittion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol? symbol, BoundCallExpression call, int tempIndex, out TextEmittionNode? node)
        {
            if (call.Function == BuiltInNamespace.Minecraft.General.RunCommand)
            {
                node = GetRunCommand(functionBuilder, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackEnable)
            {
                node = GetDatapackEnable(functionBuilder, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackDisable)
            {
                node = GetDatapackDisable(functionBuilder, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.SetDatapackEnabled)
            {
                node = GetDatapackEnabledSetter(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeather)
            {
                node = GetWeatherSetter(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForTicks)
            {
                node = GetWeatherForTicksSetter(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForDays)
            {
                node = GetWeatherForDaysSetter(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForSeconds)
            {
                node = GetWeatherForSecondsSetter(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Say)
            {
                node = GetPrint(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Print)
            {
                node = GetPrint(functionBuilder, call, tempIndex);
                return true;
            }

            if (symbol == null)
            {
                node = null;
                return false;
            }
                
            if (call.Function == BuiltInNamespace.Minecraft.General.GetDatapackCount)
            {
                node = GetDatapackCountEmittion(functionBuilder, symbol, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetEnabledDatapackCount)
            {
                node = GetDatapackCountEmittion(functionBuilder, symbol, call, true);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetAvailableDatapackCount)
            {
                node = GetDatapackCountEmittion(functionBuilder, symbol, call, false, true);
                return true;
            }

            node = null;
            return false;
        }

        private TextEmittionNode GetRunCommand(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macroCommand = Macro(functionBuilder, "command");

            if (GetOrCreateBuiltIn(call.Function, out var macro))
                macro.AddMacro(new TextCommand(macroCommand.Accessor));
        
            return Block(
                    GetAssignment(functionBuilder, macroCommand, call.Arguments.First(), 0),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode GetDatapackEnable(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macroPack = Macro(functionBuilder, "pack");

            if (GetOrCreateBuiltIn(call.Function, out var macro))
                macro.AddMacro(new DatapackEnableCommand(macroPack.Accessor));

            return Block(
                    GetAssignment(functionBuilder, macroPack, call.Arguments.First()),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode GetDatapackDisable(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macroPack = Macro(functionBuilder, "pack");

            if (GetOrCreateBuiltIn(call.Function, out var macro))
                macro.AddMacro(new DatapackDisableCommand(macroPack.Accessor)); ;

            return Block(
                    GetAssignment(functionBuilder, macroPack, call.Arguments.First()),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode GetDatapackEnabledSetter(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex)
        {
            var pack = call.Arguments[0];
            var value = call.Arguments[1];

            var macroPack = Macro(functionBuilder, "pack");
            var temp = Temp(functionBuilder, TypeSymbol.Bool, tempIndex + 1, "*de", DataLocation.Scoreboard);

            if (GetOrCreateBuiltIn(call.Function, out var macro))
            {
                macro.AddMacro(Execute().IfScoreMatches(temp, "1").Run(new ReturnRunCommand(new DatapackEnableCommand(macroPack.Accessor))));
                macro.AddMacro(new DatapackDisableCommand(macroPack.Accessor));
            }

            return Block(
                    GetAssignment(functionBuilder, macroPack, pack, tempIndex),
                    GetAssignment(functionBuilder, temp, value, tempIndex),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode GetDatapackCountEmittion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundCallExpression call, bool countEnabled = false, bool countAvailable = false)
            => GetGetterAssignment(symbol, new DatapackListCommand(
                countEnabled
                    ? DatapackListCommand.ListFilter.Enabled
                    : DatapackListCommand.ListFilter.Available));
        
        private TextEmittionNode GetWeatherSetter(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex, string? timeUnits = null)
        {
            TextEmittionNode GetNonMacroNonConstantTypeCheck(BoundExpression weatherType, int current, int time = 0, string? timeUnits = null)
            {
                var temp = Temp(functionBuilder, weatherType.Type, current, "type");
                var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

                foreach (var enumMember in BuiltInNamespace.Minecraft.General.Weather.Weather.Members)
                {
                    var intMember = (IntEnumMemberSymbol)enumMember;

                    builder.Add(Execute().IfScoreMatches(temp, intMember.UnderlyingValue.ToString())
                                         .Run(new WeatherCommand(enumMember.Name.ToLower(), time.ToString(), timeUnits)));
                }

                return new TextBlockEmittionNode(builder.ToImmutable());
            }

            var weatherType = call.Arguments[0];

            if (call.Arguments.Length > 1)
            {
                if (call.Arguments[1] is BoundLiteralExpression l)
                {
                    if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                        return new WeatherCommand(em.Name.ToLower(), l.Value.ToString(), timeUnits);
                    else
                    {
                        var time = (int)l.Value;
                        return GetNonMacroNonConstantTypeCheck(weatherType, tempIndex, time, timeUnits);
                    }
                }
                else
                {
                    var macroType = Macro(functionBuilder, "type");
                    var macroDuration = Macro(functionBuilder, "duration");
                    var macroTimeUnits = Macro(functionBuilder, "timeUnits");

                    var duration = Temp(functionBuilder, call.Arguments[1].Type, tempIndex);

                    if (GetOrCreateBuiltIn(BuiltInNamespace.Minecraft.General.Weather.SetWeather, out var macro))
                        macro.AddMacro(new WeatherCommand(macroType.Accessor, macroDuration.Accessor, macroTimeUnits.Accessor));
   
                    return Block(
                            GetAssignment(functionBuilder, macroType, weatherType, tempIndex),
                            GetAssignment(functionBuilder, duration, call.Arguments[1], tempIndex),
                            GetAssignment(functionBuilder, macroDuration, duration),
                            GetAssignment(macroTimeUnits, $"{timeUnits}"),
                            GetMacroCall(macro)
                        );
                }
            }
            else
            {
                if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                    return new WeatherCommand(em.Name.ToLower());
                else
                    return GetNonMacroNonConstantTypeCheck(weatherType, tempIndex);
            }
        }

        private TextEmittionNode GetWeatherForTicksSetter(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int current) => GetWeatherSetter(functionBuilder, call, current, "t");
        private TextEmittionNode GetWeatherForSecondsSetter(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int current) => GetWeatherSetter(functionBuilder, call, current, "s");
        private TextEmittionNode GetWeatherForDaysSetter(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int current) => GetWeatherSetter(functionBuilder, call, current, "d");

        private TextEmittionNode GetPrint(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex)
        {
            var argument = call.Arguments[0];

            if (argument is BoundLiteralExpression literal)
                return new TellrawCommand("@a", $"{{\"text\":\"{literal.Value}\"}}");
            else if (argument is BoundVariableExpression variable)
            {
                var varSymbol = ToEmittionVariable(functionBuilder, variable.Variable, false, true);
                return new TellrawCommand("@a", $"{{\"storage\":\"{MainStorage}\",\"nbt\":\"{varSymbol.SaveName}\"}}");
            }
            else
            {
                var temp = Temp(functionBuilder, argument.Type, 0);

                return Block(
                        GetAssignment(functionBuilder, temp, argument, tempIndex),
                        new TellrawCommand("@a", $"{{\"storage\":\"{MainStorage}\",\"nbt\":\"{temp.SaveName}\"}}")
                    );
            }
        }
    }
}
