﻿using Blaze.Binding;
using Blaze.Emit.NameTranslation;
using Blaze.Emit.Nodes;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using static Blaze.Symbols.EmittionVariableSymbol;

namespace Blaze.Emit
{
    internal sealed class DatapackBuilder
    {
        private const int DEBUG_CHUNK_X = 10000000;
        private const int DEBUG_CHUNK_Z = 10000000;
        private const string CONST = "CONST";
        private static readonly UUID _mathEntity1 = new UUID(1068730519, 377069937, 1764794166, -1230438844);
        private static readonly UUID _mathEntity2 = new UUID(-1824770608, 1852200875, -1037488134, 520770809);
        private static readonly EmittionVariableSymbol _macro = new EmittionVariableSymbol("*macros", TypeSymbol.Object, true);
        private static readonly EmittionVariableSymbol _returnValue = new EmittionVariableSymbol("*return.value", TypeSymbol.Object, false);
        
        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;

        private readonly MinecraftFunction.Builder _initFunction;
        private readonly MinecraftFunction.Builder _tickFunction;

        private readonly List<FunctionSymbol> _fabricatedMacroFunctions = new List<FunctionSymbol>();
        private readonly Dictionary<FunctionSymbol, MinecraftFunction.Builder> _usedBuiltIn = new Dictionary<FunctionSymbol, MinecraftFunction.Builder>();

        private EmittionVariableSymbol? _thisSymbol = null;

        public string Vars => $"{_configuration.RootNamespace}.vars";
        public string MainStorage => $"{_configuration.RootNamespace}:main";
        
        public DatapackBuilder(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _configuration = configuration;

            _initFunction = MinecraftFunction.Init(_configuration.RootNamespace, program.GlobalNamespace);
            _tickFunction = MinecraftFunction.Tick(_configuration.RootNamespace, program.GlobalNamespace);
        }


        //These are honestly just for looks, it makes for readable blocks
        private TextEmittionNode GetCallWith(MinecraftFunction.Builder function, string jsonLiteral) => FunctionCommand.GetCallWith(function.CallName, jsonLiteral);
        private TextEmittionNode GetCallWith(MinecraftFunction.Builder function, EmittionVariableSymbol symbol) => FunctionCommand.GetCallWith(function.CallName, MainStorage, symbol);
        private TextEmittionNode GetMacroCall(MinecraftFunction.Builder function) => FunctionCommand.GetCallWith(function.CallName, MainStorage, _macro);
        private TextEmittionNode GetCall(MinecraftFunction.Builder function) => FunctionCommand.GetCall(function.CallName);
        private TextEmittionNode GetCall(FunctionSymbol function) => FunctionCommand.GetCall($"{_configuration.RootNamespace}:{function.AddressName}");

        private TextEmittionNode ScoreSet(string variable, int value) => ScoreboardCommand.SetScore(variable, Vars, value.ToString());
        private TextEmittionNode ScoreSet(EmittionVariableSymbol variable, int value) => ScoreboardCommand.SetScore(variable.SaveName, Vars, value.ToString());
        private TextEmittionNode ScoreGet(string variable) => ScoreboardCommand.GetScore(variable, Vars, null);
        private TextEmittionNode ScoreGet(EmittionVariableSymbol variable) => ScoreboardCommand.GetScore(variable.SaveName, Vars, null);
        private TextEmittionNode ScoreAdd(EmittionVariableSymbol variable, int value) => ScoreboardCommand.ScoreAdd(variable.SaveName, Vars, value.ToString());
        private TextEmittionNode ScoreSubtract(EmittionVariableSymbol variable, int value) => ScoreboardCommand.ScoreSubtract(variable.SaveName, Vars, value.ToString());
        private TextEmittionNode ScoreReset(EmittionVariableSymbol variable) => ScoreboardCommand.ScoreReset(variable.SaveName, Vars);
        private TextEmittionNode ScoreOperation(EmittionVariableSymbol left, BoundBinaryOperatorKind operatorKind, EmittionVariableSymbol right) => ScoreboardCommand.ScoreOperation(left.SaveName, Vars, EmittionFacts.ToPlayersOperation(operatorKind), right.SaveName, Vars);
        private TextEmittionNode ScoreCopy(EmittionVariableSymbol left, EmittionVariableSymbol right) => ScoreboardCommand.ScoreOperation(left.SaveName, Vars, ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause.PlayersOperation.Assignment, right.SaveName, Vars);
        private TextEmittionNode LineBreak() => TextTriviaNode.LineBreak();
        private TextEmittionNode Comment(string comment) => TextTriviaNode.Comment(comment);


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
            _initFunction.AddCommand($"forceload add {DEBUG_CHUNK_X} {DEBUG_CHUNK_Z}");
            _initFunction.AddCommand($"kill @e[tag=debug,tag=blz]");
            _initFunction.AddCommand($"summon item_display {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z} {{Tags:[\"blz\",\"debug\", \"first\"], UUID:{_mathEntity1.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:1,less:0}}}}}}}}");
            _initFunction.AddCommand($"summon item_display {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z} {{Tags:[\"blz\",\"debug\", \"second\"], UUID:{_mathEntity2.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:0,less:1}}}}}}}}");
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

                    var builder = _usedBuiltIn[function];

                    //if (function.ReturnType != TypeSymbol.Void)
                    //    builder.Content.Insert(0, GetResetCommand(_returnValue));
                    
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
                return new TextBlockEmittionNode(builder.ToImmutable(), true);
            }
            else
            {
                return TextTriviaNode.Empty();
            }
        }

        private TextEmittionNode GetResetCommand(EmittionVariableSymbol local)
        {
            switch (local.Location)
            {
                case EmittionVariableLocation.Scoreboard:
                    return ScoreReset(local);
                case EmittionVariableLocation.Storage:
                    return new TextCommand($"data remove storage {MainStorage} {local.SaveName}", true);
                default:
                    throw new Exception($"Unexpected variable location {local.Location}");
            }
        }

        private TextEmittionNode GetStatement(MinecraftFunction.Builder functionBuilder, BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.NopStatement:
                    return GetNopStatement();
                case BoundNodeKind.BlockStatement:
                    return GetBlockStatement(functionBuilder, (BoundBlockStatement)node);
                case BoundNodeKind.ExpressionStatement:
                    return GetExpressionStatement(functionBuilder, (BoundExpressionStatement)node);
                case BoundNodeKind.VariableDeclarationStatement:
                    return GetVariableDeclarationStatement(functionBuilder, (BoundVariableDeclarationStatement)node);
                case BoundNodeKind.IfStatement:
                    return GetIfStatement(functionBuilder, (BoundIfStatement)node);
                case BoundNodeKind.WhileStatement:
                    return GetWhileStatement(functionBuilder, (BoundWhileStatement)node);
                case BoundNodeKind.DoWhileStatement:
                    return GetDoWhileStatement(functionBuilder, (BoundDoWhileStatement)node);
                case BoundNodeKind.BreakStatement:
                    return GetBreakStatement(functionBuilder, (BoundBreakStatement)node);
                case BoundNodeKind.ContinueStatement:
                    return GetContinueStatement(functionBuilder, (BoundContinueStatement)node);
                case BoundNodeKind.ReturnStatement:
                    return GetReturnStatement(functionBuilder, (BoundReturnStatement)node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
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
            //Emit condition into <.temp>
            //execute if <.temp> run subfunction
            //else generate a sub function and run it instead
            //if there is an else clause generate another sub with the else body

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var subFunction = functionBuilder.CreateSub(SubFunctionKind.If);
            subFunction.Content.Add(GetStatement(subFunction, node.Body));

            var temp = Temp(functionBuilder, TypeSymbol.Bool, 0);

            if (node.ElseBody == null)
            {
                return Block(
                        GetAssignment(functionBuilder, temp, node.Condition, 0),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run function {subFunction.CallName}", false)
                    );
            }   
            else
            {
                var elseSubFunction = functionBuilder.CreateSub(SubFunctionKind.Else);
                elseSubFunction.Content.Add(GetStatement(elseSubFunction, node.ElseBody));

                return Block(
                        GetAssignment(functionBuilder, temp, node.Condition, 0),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run function {subFunction.CallName}", false),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run function {elseSubFunction.CallName}", false)
                    );
            }
        }

        private TextEmittionNode GetWhileStatement(MinecraftFunction.Builder functionBuilder, BoundWhileStatement node)
        {
            //Main:
            //Call sub function
            //
            //Generate body sub function:
            //Emit condition into <.temp>
            //execute if <.temp> run return 0
            //body

            var subFunction = functionBuilder.CreateSub(SubFunctionKind.Loop);
            var temp = Temp(functionBuilder, TypeSymbol.Bool, 0);

            var callCommand = GetCall(subFunction);

            subFunction.Content.Add(GetAssignment(subFunction, temp, node.Condition));
            subFunction.Content.Add(new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run return 0", false));
            subFunction.AddLineBreak();
            subFunction.Content.Add(GetStatement(subFunction, node.Body));
            subFunction.AddLineBreak();
            subFunction.Content.Add(callCommand);

            return callCommand;
        }

        private TextEmittionNode GetDoWhileStatement(MinecraftFunction.Builder functionBuilder, BoundDoWhileStatement node)
        {
            //Main:
            //Call sub function
            //
            //Generate body sub function:
            //body
            //Emit condition into <.temp>
            //execute if <.temp> run function <subfunction>

            var subFunction = functionBuilder.CreateSub(SubFunctionKind.Loop);
            var temp = Temp(functionBuilder, TypeSymbol.Bool, 0);

            var callCommand = GetCall(subFunction);

            subFunction.Content.Add(GetAssignment(subFunction, temp, node.Condition, 0));
            subFunction.AddLineBreak();
            subFunction.Content.Add(GetStatement(subFunction, node.Body));
            subFunction.Content.Add(new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run function {subFunction.CallName}", false));
            
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
            //Emit cleanup before we break the function
            //Assign the return value to <return.value>
            //If the return value is an integer or a bool, return it

            var returnExpression = node.Expression;
            functionBuilder.Locals.AddRange(functionBuilder.Scope.GetLocals());

            if (returnExpression == null)
            {
                return Block(
                        GetCleanUp(functionBuilder, functionBuilder.Scope.GetLocals()),
                        new TextCommand("return 0", false)
                    );
            }

            return Block(
                    GetCleanUp(functionBuilder, functionBuilder.Scope.GetLocals()),
                    GetAssignment(functionBuilder, _returnValue, returnExpression, 0),
                    new TextCommand($"return run data get storage {MainStorage} {_returnValue.SaveName} 1", false)
                );
        }

        private TextEmittionNode GetExpressionStatement(MinecraftFunction.Builder functionBuilder, BoundExpressionStatement node)
        {
            //Can be either call or assignment
            var expression = node.Expression;

            if (expression is BoundAssignmentExpression assignment)
            {
                return GetAssignment(functionBuilder, assignment, 0);
            }
            else if (expression is BoundCallExpression call)
            {
                return GetCallExpression(functionBuilder, null, call, 0);
            }
            else
            {
                throw new Exception($"Unexpected expression statement kind {expression.Kind}");
            }
        }

        private TextEmittionNode GetCallExpression(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol? symbol, BoundCallExpression call, int current)
        {
            //Can be a built-in function -> TryEmitBuiltInFunction();
            //Can be a user defined function ->
            //
            //Assign every parameter to temp variable
            //function <function>
            //Reset every parameter

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

        private TextEmittionNode GetNopStatement()
        {
            return new TextCommand("tellraw @a {\"text\":\"Nop statement in program\", \"color\":\"red\"}", false);
        }

        private TextEmittionNode GetAssignment(MinecraftFunction.Builder functionBuilder, BoundAssignmentExpression assignment, int current)
            => GetAssignment(functionBuilder, assignment.Left, assignment.Right, current);

        private TextEmittionNode GetAssignment(MinecraftFunction.Builder functionBuilder, BoundExpression left, BoundExpression right, int tempIndex)
        {
            if (left is BoundFieldAccessExpression fieldAccess)
            {
                if (TryEmitBuiltInFieldAssignment(functionBuilder, fieldAccess.Field, right, tempIndex, out var node))
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

                if (nameYield.Length > 0)
                {
                    var macroLeft = Macro(functionBuilder, "left");
                    var literal = new BoundLiteralExpression(nameYield.ToString());
                    builder.Add(GetStringConcatenation(functionBuilder, macroLeft, literal, tempIndex));
                }

                builder.Add(GetMacroAssignment(functionBuilder, right, tempIndex));
                return new TextBlockEmittionNode(builder.ToImmutable());
            }
            else
            {
                //Left expression did not require macro emittion so we just lookup a symbol and assign right to it

                var resultName = nameYield.ToString();
                var leftVar = functionBuilder.Scope.LookupOrDeclare(resultName, left.Type, false, false, enforceStorage ? EmittionVariableLocation.Storage : null);
                builder.Add(GetAssignment(functionBuilder, leftVar, right, tempIndex));
                return new TextBlockEmittionNode(builder.ToImmutable());
            }
        }

        private TextEmittionNode GetArrayAccessAssignment(MinecraftFunction.Builder functionBuilder, string nameYield, bool isSetter, BoundArrayAccessExpression arrayAccess, int tempIndex)
        {
            var rank = arrayAccess.Arguments.Length;
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var macroName = Macro(functionBuilder, "name");

            builder.Add(GetAssignment(macroName, $"\"*{nameYield.ToString()}\""));

            MinecraftFunction.Builder fabricatedAccessor;

            if (isSetter)
            {
                var macroLeft = Macro(functionBuilder, "left");
                
                if (GetFabricatedFunction($"set_array_access_rank{rank}", TypeSymbol.Void, out fabricatedAccessor))
                {
                    var accessRankBuilder = new StringBuilder();

                    for (int i = 0; i < rank; i++)
                        accessRankBuilder.Append($"[$({"a" + i.ToString()})]");

                    fabricatedAccessor.AddMacro($"data modify storage {MainStorage} {macroLeft.SaveName} set value \"$(name){accessRankBuilder.ToString()}\"");
                }
            }
            else
            {
                if (GetFabricatedFunction($"get_array_access_rank{rank}", TypeSymbol.Object, out fabricatedAccessor))
                {
                    var accessRankBuilder = new StringBuilder();

                    for (int i = 0; i < rank; i++)
                        accessRankBuilder.Append($"[$({"a" + i.ToString()})]");

                    fabricatedAccessor.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set from storage {MainStorage} $(name){accessRankBuilder.ToString()}");
                }
            }

            for (int i = 0; i < rank; i++)
            {
                var macroArg = Macro(functionBuilder, $"a{i.ToString()}");
                var temp = Temp(functionBuilder, TypeSymbol.Int, tempIndex + i);

                builder.Add(GetAssignment(functionBuilder, temp, arrayAccess.Arguments[i], tempIndex + i));
                builder.Add(new TextCommand($"execute store result storage {MainStorage} {macroArg.SaveName} int 1 run scoreboard players get {temp.SaveName} {Vars}", false));
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

            var rightVar = functionBuilder.Scope.LookupOrDeclare(resultName, right.Type, false, false, enforceStorage ? EmittionVariableLocation.Storage : null);

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
            if (TryEmitBuiltInFieldGetter(functionBuilder, symbol, fieldAccess, tempIndex, out var node))
            {
                Debug.Assert(node != null);
                return node;
            }

            return GetPrimaryAssignment(functionBuilder, symbol, fieldAccess, tempIndex);
        }

        private TextEmittionNode GetAssignment(EmittionVariableSymbol symbol, object value)
        {
            if (symbol.Location == EmittionVariableLocation.Storage)
            {
                if (value is int)
                {
                    return new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set value {value}", false);
                }
                else if (value is bool)
                {
                    int numberValue = (bool)value ? 1 : 0;
                    return new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set value {value}", false);
                }
                else if (value is string)
                {
                    var str = (string)value;
                    return new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set value \"{str.Replace("\"", "\\\"")}\"", false);
                }
                else if (value is float)
                {
                    return new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set value {value}f", false);
                }
                else if (value is double)
                {
                    return new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set value {value}d", false);
                }
                else
                {
                    throw new Exception($"Unexpected storage stored literal type {value.GetType()}");
                }

            }
            else if (symbol.Location == EmittionVariableLocation.Scoreboard)
            {
                if (value is bool)
                {
                    return ScoreSet(symbol, (bool)value ? 1 : 0);
                }
                else if (value is int)
                {
                    return ScoreSet(symbol, (int)value);
                }
                else
                    throw new Exception($"Unexpected scoreboard literal type {value.GetType()}");
            }
            else
                throw new Exception($"Unexpected emittion variable location {symbol.Location}");
        }

        private TextEmittionNode GetLiteralAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundLiteralExpression literal) => GetAssignment(symbol, literal.Value);

        private TextEmittionNode GetVariableAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundVariableExpression variable)
        {
            if (variable.Variable is StringEnumMemberSymbol stringEnumMember)
            {
                return GetAssignment(symbol, stringEnumMember.UnderlyingValue);
            }
            else if (variable.Variable is IntEnumMemberSymbol intEnumMember)
            {
                return GetAssignment(symbol, intEnumMember.UnderlyingValue);
            }
            else
            {
                var rightVariable = ToEmittionVariable(functionBuilder, variable.Variable, false, !(variable.Variable is ParameterSymbol));
                return GetAssignment(functionBuilder, symbol, rightVariable);
            }
        }

        private TextEmittionNode GetAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            //TODO: return empty trivia when two readonly variables are being assigned

            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string, float, double literal    -> data modify storage strings *this set from storage strings *other
            //named type        -> assign all the fields to the corresponding ones of the object we are copying

            if (left == right)
                return TextTriviaNode.Empty();

            if (left.Location == EmittionVariableLocation.Storage && right.Location == EmittionVariableLocation.Storage)
            {
                return new TextCommand($"data modify storage {MainStorage} {left.SaveName} set from storage {MainStorage} {right.SaveName}", false);
            }

            if (left.Location == EmittionVariableLocation.Scoreboard && right.Location == EmittionVariableLocation.Scoreboard)
            {
                return ScoreCopy(left, right);
            }

            if (left.Location == EmittionVariableLocation.Storage && right.Location == EmittionVariableLocation.Scoreboard)
            {
                return new TextCommand($"execute store result storage {MainStorage} {left.SaveName} int 1 run scoreboard players get {right.SaveName} {Vars}", false);
            }

            if (left.Location == EmittionVariableLocation.Scoreboard && right.Location == EmittionVariableLocation.Storage)
            {
                return new TextCommand($"execute store result score {left.SaveName} {Vars} run data get storage {MainStorage} {right.SaveName}", false);
            }

            throw new Exception($"Unexpected variable location combination {left.Location} and {right.Location}");
        }

        private TextEmittionNode GetMacroAssignment(MinecraftFunction.Builder functionBuilder, BoundExpression right, int tempIndex)
        {
            var rightVar = new EmittionVariableSymbol("*right", right.Type, true, null, null);
            functionBuilder.Scope.Declare(rightVar);

            MinecraftFunction.Builder assignmentMacro;

            if (rightVar.Location == EmittionVariableLocation.Storage)
            {
                if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.AssignStSt, out assignmentMacro))
                {
                    assignmentMacro.AddMacro($"data modify storage {MainStorage} $(left) set from storage {MainStorage} {rightVar.SaveName}");
                }
            }
            else
            {
                if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.AssignStSc, out assignmentMacro))
                {
                    assignmentMacro.AddMacro($"execute store result storage {MainStorage} $(left) int 1 run scoreboard players get {rightVar.SaveName} {Vars}");
                }
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
            //Reserve a name for an object
            //Execute the constructor with the arguments

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

            var constructor = objectCreationExpression.NamedType.Constructor;
            Debug.Assert(constructor != null);
            Debug.Assert(constructor.FunctionBody != null);

            //We do this so that the constructor block knows the "this" instance name
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
            //in a for loop, append a default value to a tempI list
            //in a for loop, append result of that to a temp[i-1] list
            //if i == 0, don't use a temp list: use the desired name
            //Remove all the temp lists

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var defaultValue = EmittionFacts.GetEmittionDefaultValue(arrayCreationExpression.ArrayType.Type);
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

                if (i == arrayCreationExpression.Dimensions.Length - 1)
                {
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
                        assignmentCommand = GetAssignment(arraySymbol, initializerBuilder.ToString());
                        builder.Add(assignmentCommand);
                        previous = arraySymbol;
                        continue;
                    }
                    else
                        assignmentCommand = new TextCommand($"data modify storage {MainStorage} {arraySymbol.SaveName} append value {defaultValue}", false);
                }
                else
                    assignmentCommand = new TextCommand($"data modify storage {MainStorage} {arraySymbol.SaveName} append from storage {MainStorage} {previous?.SaveName}", false);

                builder.Add(GetAssignment(arraySymbol, "[]"));

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
                subFunction.Content.Add(ScoreAdd(tempIter, 1));
                subFunction.AddCommand($"execute if score {tempIter.SaveName} {Vars} < {tempUpperBound.SaveName} {Vars} run function {subFunction.CallName}");

                previous = arraySymbol;
            }
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        

        private TextEmittionNode GetUnaryExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundUnaryExpression unary, int tempIndex)
        {
            //TODO: Add constant assignment to load function

            //Identity -> Assign the expression normally
            //Negation -> Assign the expression normally, than multiply it by -1
            //Logical negation
            //         -> Assign the expression to <.temp> variable
            //            If it is 1, set the <*name> to 0
            //            If it is 0, set the <*name> to 1

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
                    {
                        EmittionVariableSymbol operandSymbol;

                        if (symbol.Location == EmittionVariableLocation.Storage)
                        {
                            operandSymbol = Temp(functionBuilder, operand.Type, tempIndex);
                        }
                        else
                        {
                            operandSymbol = symbol;
                        }

                        var minusOneConst = new EmittionVariableSymbol("-1", TypeSymbol.Int, false);

                        return Block(
                                LineBreak(),
                                Comment($"Emitting integer negation unary expression \"{unary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, operandSymbol, operand, tempIndex),

                                symbol.Location == EmittionVariableLocation.Scoreboard
                                    ? ScoreOperation(symbol, BoundBinaryOperatorKind.Multiplication, minusOneConst)
                                    : new TextCommand($"execute store result storage {MainStorage} {symbol.SaveName} int -1 run scoreboard players get {operandSymbol.SaveName} {Vars}", false)
                            );
                    }
                    else
                    {
                        var macroA = Macro(functionBuilder, "a");
                        var macroSign = Macro(functionBuilder, "sign");

                        var sign = Temp(functionBuilder, TypeSymbol.Object, tempIndex, "sign");
                        var last = Temp(functionBuilder, TypeSymbol.Object, tempIndex, "last");

                        MinecraftFunction.Builder macro;

                        if (operand.Type == TypeSymbol.Float)
                        {
                            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateFloat, out macro))
                            {
                                macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value $(sign)$(a)f");
                            }
                        }
                        else
                        {
                            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateDouble, out macro))
                            {
                                macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value $(sign)$(a)d");
                            }   
                        }

                        var typeSuffix = operand.Type == TypeSymbol.Float ? "f" : "d";

                        return Block(
                                LineBreak(),
                                Comment($"Emitting floating point negation unary expression \"{unary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, symbol, operand, tempIndex),
                                GetAssignment(functionBuilder, macroA, symbol),
                                new TextCommand($"data modify storage {MainStorage} {sign.SaveName} set string storage {MainStorage} {symbol.SaveName} 0 1", false),
                                new TextCommand($"data modify storage {MainStorage} {last.SaveName} set string storage {MainStorage} {symbol.SaveName} -1", false),
                                new TextCommand($"execute if data storage {MainStorage} {{ \"{sign.SaveName}\": \"-\"}} run data modify storage {MainStorage} {macroA.SaveName} set string storage {MainStorage} {macroA.SaveName} 1", false),
                                new TextCommand($"execute if data storage {MainStorage} {{ \"{last.SaveName}\": \"{typeSuffix}\"}} run data modify storage {MainStorage} {macroA.SaveName} set string storage {MainStorage} {macroA.SaveName} 0 -1", false),
                                new TextCommand($"execute if data storage {MainStorage} {{ \"{sign.SaveName}\": \"-\"}} run data modify storage {MainStorage} {macroSign.SaveName} set value \"\"", false),
                                new TextCommand($"execute unless data storage {MainStorage} {{ \"{sign.SaveName}\": \"-\"}} run data modify storage {MainStorage} {macroSign.SaveName} set value \"-\"", false),
                                GetMacroCall(macro),
                                GetAssignment(functionBuilder, symbol, _returnValue)
                            );
                    }
                case BoundUnaryOperatorKind.LogicalNegation:

                    var temp = Temp(functionBuilder, operand.Type, tempIndex);

                    if (symbol.Location == EmittionVariableLocation.Scoreboard)
                    {
                        return Block(
                            LineBreak(),
                            Comment($"Emitting logical negation \"{unary}\" to \"{symbol.SaveName}\""),
                            GetAssignment(functionBuilder, temp, operand, tempIndex),
                            new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 0", false),
                            new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
                        );
                    }
                    else
                    {
                        return Block(
                            LineBreak(),
                            Comment($"Emitting logical negation \"{unary}\" to \"{symbol.SaveName}\""),
                            GetAssignment(functionBuilder, temp, operand, tempIndex),
                            new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run data modify storage {MainStorage} {symbol.SaveName} set value 0", false),
                            new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run data modify storage {MainStorage} {symbol.SaveName} set value 1", false)
                        );
                    }

                default:
                    throw new Exception($"Unexpected unary operator kind {operatorKind}");
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
                            return GetStringConcatenation(functionBuilder, symbol, left, right, tempIndex);
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
                                new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches 1 if score {rightSymbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
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
                                new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 1", false),
                                new TextCommand($"execute if score {rightSymbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
                            );
                    }
                case BoundBinaryOperatorKind.Equals:
                    {
                        if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object || left.Type is EnumSymbol e && !e.IsIntEnum)
                        {
                            var leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, $"lbTemp");
                            var rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, $"rbTemp");
                            var resultTemp = Temp(functionBuilder, left.Type, tempIndex + 1);

                            return Block(
                                    LineBreak(),
                                    Comment($"Emitting equals operation \"{binary}\" to \"{symbol.SaveName}\""),
                                    GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                    GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                    new TextCommand($"execute store success score {resultTemp.SaveName} {Vars} run data modify storage {MainStorage} {leftSymbol.SaveName} set from storage {MainStorage} {rightSymbol.SaveName}", false),
                                    new TextCommand($"execute if score {resultTemp.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 0", false),
                                    new TextCommand($"execute if score {resultTemp.SaveName} {Vars} matches 0 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
                                );
                        }
                        else if (left.Type == TypeSymbol.Float || left.Type == TypeSymbol.Double)
                        {
                            return EmitFloatingPointComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                        }
                        else
                        {
                            return EmitIntComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
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
                                    new TextCommand($"execute store success score {symbol.SaveName} {Vars} run data modify storage {MainStorage} {leftSymbol.SaveName} set from storage {MainStorage} {rightSymbol.SaveName}", false)
                                );
                        }
                        else
                        {
                            return EmitIntComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                        }
                    }
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.LessOrEquals:
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    if (left.Type == TypeSymbol.Int)
                    {
                        return EmitIntComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                    }
                    else
                    {
                        return EmitFloatingPointComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                    }
                default:
                    throw new Exception($"Unexpected binary operator kind {operatorKind} for types {left.Type} and {right.Type}");
            }
        }

        private TextEmittionNode GetStringConcatenation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression right, int tempIndex)
        {
            var macroLeft = Macro(functionBuilder, "str_left");
            var macroRight = Macro(functionBuilder, "right");

            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.StrConcat, out var macro))
            {
                macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value \"$(str_left)$(right)\"");
            }   

            return Block(
                    GetAssignment(functionBuilder, macroLeft, symbol),
                    GetAssignment(functionBuilder, macroRight, right, tempIndex),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, symbol, _returnValue)
                );
        }

        private TextEmittionNode GetStringConcatenation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, int tempIndex)
        {
            var macroLeft = Macro(functionBuilder, "str_left");
            var macroRight = Macro(functionBuilder, "right");
            
            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.StrConcat, out var macro))
            {
                macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value \"$(str_left)$(right)\"");
            }   

            return Block(
                    GetAssignment(functionBuilder, symbol, left),
                    GetAssignment(functionBuilder, macroLeft, symbol),
                    GetAssignment(functionBuilder, macroRight, right, tempIndex),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, symbol, _returnValue)
                );
        }

        private TextEmittionNode GetIntBinaryAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int tempIndex)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            EmittionVariableSymbol leftSymbol;
            EmittionVariableSymbol? rightSymbol = null;

            if (symbol.Location == EmittionVariableLocation.Storage)
            {
                leftSymbol = Temp(functionBuilder, TypeSymbol.Int, tempIndex + 1);
            }
            else
            {
                leftSymbol = symbol;
            }

            builder.Add(GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1));

            if (right is BoundLiteralExpression l)
            {
                if (operation == BoundBinaryOperatorKind.Addition)
                {
                    builder.Add(ScoreAdd(leftSymbol, (int) l.Value));

                    if (symbol.Location == EmittionVariableLocation.Storage)
                        builder.Add(GetAssignment(functionBuilder, symbol, leftSymbol));

                    return new TextBlockEmittionNode(builder.ToImmutable());
                }
                else if (operation == BoundBinaryOperatorKind.Subtraction)
                {
                    builder.Add(ScoreSubtract(leftSymbol, (int)l.Value));

                    if (symbol.Location == EmittionVariableLocation.Storage)
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

                if (rightExpressionVar.Location == EmittionVariableLocation.Scoreboard)
                {
                    rightSymbol = rightExpressionVar;
                }
            }

            if (rightSymbol == null)
            {
                rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, "rTemp");
                builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1));
            }

            builder.Add(ScoreOperation(leftSymbol, operation, rightSymbol));

            if (symbol.Location == EmittionVariableLocation.Storage)
                builder.Add(GetAssignment(functionBuilder, symbol, leftSymbol));

            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode EmitIntComparisonOperation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int tempIndex)
        {
            EmittionVariableSymbol? leftSymbol = null;

            var isInverted = operatorKind == BoundBinaryOperatorKind.NotEquals;
            var initialValue = isInverted ? 1 : 0;
            var successValue = isInverted ? 0 : 1;

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

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

                if (leftExpressionSymbol.Location == EmittionVariableLocation.Scoreboard)
                {
                    leftSymbol = leftExpressionSymbol;
                }
            }

            if (leftSymbol == null)
            {
                leftSymbol = Temp(functionBuilder, left.Type, tempIndex + 1, "lTemp");
                builder.Add(GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1));
            }

            builder.Add(ScoreSet(symbol, initialValue));

            if (right is BoundLiteralExpression l && l.Value is int)
            {
                int value = (int)l.Value;
                var range = operatorKind switch
                {
                    BoundBinaryOperatorKind.Less => ".." + (value - 1).ToString(),
                    BoundBinaryOperatorKind.LessOrEquals => ".." + value,
                    BoundBinaryOperatorKind.Greater => (value + 1).ToString() + "..",
                    BoundBinaryOperatorKind.GreaterOrEquals => value + "..",
                    BoundBinaryOperatorKind.Equals => value.ToString(),
                    BoundBinaryOperatorKind.NotEquals => value.ToString(),
                    _ => throw new Exception($"Unexpected literal operator kind {operatorKind.ToString()}")
                };

                builder.Add(new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches {range} run scoreboard players set {symbol.SaveName} {Vars} {successValue}", false));
            }
            else
            {
                EmittionVariableSymbol? rightSymbol = null;

                if (right is BoundVariableExpression vr)
                {
                    if (vr.Variable is IntEnumMemberSymbol enumMember)
                    {
                        var memberUnderlyingValue = enumMember.UnderlyingValue;
                        builder.Add(new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches {memberUnderlyingValue} run scoreboard players set {symbol.SaveName} {Vars} {successValue}", false));
                        return new TextBlockEmittionNode(builder.ToImmutable());
                    }

                    var rightExpressionVariable = ToEmittionVariable(functionBuilder, vr.Variable, false, true);

                    if (rightExpressionVariable.Location == EmittionVariableLocation.Scoreboard)
                    {
                        rightSymbol = rightExpressionVariable;
                    }
                }

                if (rightSymbol == null)
                {
                    rightSymbol = Temp(functionBuilder, right.Type, tempIndex + 1, "rTemp");
                    builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1));
                }

                var operationSign = EmittionFacts.GetComparisonSign(operatorKind);
                builder.Add(new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} {operationSign} {rightSymbol.SaveName} {Vars} run scoreboard players set {symbol.SaveName} {Vars} {successValue}", false));
            }

            return new TextBlockEmittionNode(builder.ToImmutable());
        }
        private TextEmittionNode EmitFloatingPointComparisonOperation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int current)
        {
            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.PositionY, out var macro))
            {
                macro.AddMacro($"tp @s {DEBUG_CHUNK_X} $(a) {DEBUG_CHUNK_Z}");
            }   

            var macroA = Macro(functionBuilder, "a");
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            builder.Add(GetAssignment(functionBuilder, macroA, left, current));
            builder.Add(new TextCommand($"execute as {_mathEntity1} run function {macro.CallName} with storage {MainStorage} {_macro.SaveName}", false));

            builder.Add(GetAssignment(functionBuilder, macroA, right, current));
            builder.Add(new TextCommand($"execute as {_mathEntity2} run function {macro.CallName} with storage {MainStorage} {_macro.SaveName}", false));

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Equals:
                    builder.Add(new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} 0", false));
                    builder.Add(new TextCommand($"execute as {_mathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 1", false));
                    break;

                case BoundBinaryOperatorKind.NotEquals:
                    builder.Add(new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} 1", false));
                    builder.Add(new TextCommand($"execute as {_mathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 0", false));
                    break;

                case BoundBinaryOperatorKind.Greater:
                    builder.Add(new TextCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this", false));
                    builder.Add(new TextCommand($"execute store result score {symbol.SaveName} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".greater", false));
                    builder.Add(new TextCommand($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 0", false));
                    builder.Add(new TextCommand($"tag @e[tag=.this] remove .this", false));
                    break;

                case BoundBinaryOperatorKind.Less:
                    builder.Add(new TextCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this", false));
                    builder.Add(new TextCommand($"execute store result score {symbol.SaveName} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".less", false));
                    builder.Add(new TextCommand($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 0", false));
                    builder.Add(new TextCommand($"tag @e[tag=.this] remove .this", false));
                    break;

                case BoundBinaryOperatorKind.GreaterOrEquals:
                    builder.Add(new TextCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this", false));
                    builder.Add(new TextCommand($"execute store result score {symbol.SaveName} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".greater", false));
                    builder.Add(new TextCommand($"tag @e[tag=.this] remove .this", false));
                    break;

                case BoundBinaryOperatorKind.LessOrEquals:
                    builder.Add(new TextCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this", false));
                    builder.Add(new TextCommand($"execute store result score {symbol.SaveName} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".less", false));
                    builder.Add(new TextCommand($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 1", false));
                    builder.Add(new TextCommand($"tag @e[tag=.this] remove .this", false));
                    break;
            }

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
                        {
                            macro.AddMacro($"execute positioned ~ $(a) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(b) {DEBUG_CHUNK_Z}");
                        }
                        break;
                    }
                case BoundBinaryOperatorKind.Subtraction:
                    {
                        var last = functionBuilder.Scope.LookupOrDeclare("*last", TypeSymbol.Object, true, false);
                        var pol = functionBuilder.Scope.LookupOrDeclare("*pol", TypeSymbol.Object, true, false);

                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Subtract, out macro))
                        {
                            macro.AddMacro($"execute positioned ~ $(b) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(polarity)$(a) {DEBUG_CHUNK_Z}");
                        }

                        if (macro.GetOrCreateSub("if_minus", out var sub))
                        {
                            sub.AddCommand($"data modify storage {MainStorage} {macroA.SaveName} set string storage {MainStorage} {macroA.SaveName} 1");
                            sub.AddCommand($"data modify storage {MainStorage} {last.SaveName} set string storage {MainStorage} {macroA.SaveName} -1");
                            sub.AddCommand($"execute if data storage {MainStorage} {{ \"{last.SaveName}\" : \"d\" }} run data modify storage {MainStorage} {macroA.SaveName} set string storage {MainStorage} {macroA.SaveName} 0 -1");
                            sub.AddCommand($"execute if data storage {MainStorage} {{ \"{last.SaveName}\" : \"f\" }} run data modify storage {MainStorage} {macroA.SaveName} set string storage {MainStorage} {macroA.SaveName} 0 -1");
                            sub.Content.Add(GetAssignment(macroPolarity, "\"\""));
                        }

                        blockBuilder.Add(GetAssignment(functionBuilder, macroB, symbol));
                        blockBuilder.Add(GetAssignment(functionBuilder, macroA, rightSymbol));
                        blockBuilder.Add(GetAssignment(functionBuilder, pol, rightSymbol));
                        blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {pol.SaveName} set string storage {MainStorage} {pol.SaveName} 0 1", false));
                        blockBuilder.Add(new TextCommand($"execute if data storage {MainStorage} {{ \"{pol.SaveName}\" : \"-\" }} run function {sub.CallName}", false));
                        blockBuilder.Add(new TextCommand($"execute unless data storage {MainStorage} {{ \"{pol.SaveName}\" : \"-\" }} run data modify storage {MainStorage} {macroPolarity.SaveName} set value \"-\"", false));

                        blockBuilder.Add(GetDoubleConversion(functionBuilder, macroA, macroA));
                        break;
                    }
                case BoundBinaryOperatorKind.Multiplication:
                    {
                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Multiply, out macro))
                        {
                            macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f]");
                            macro.AddMacro($"data modify entity {entity} transformation set value [0f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,$(b)f]");
                        }
                        break;
                    }
                case BoundBinaryOperatorKind.Division:
                    {
                        if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Divide, out macro))
                        {
                            macro.AddMacro($"data modify entity {entity} transformation set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,$(b)f]");
                        }   
                        break;
                    }

                default:
                    throw new Exception($"Unexpected binary operation kind {kind}");
            }

            blockBuilder.Add(GetMacroCall(macro));

            if (kind == BoundBinaryOperatorKind.Addition || kind == BoundBinaryOperatorKind.Subtraction)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set from entity {_mathEntity1.ToString()} Pos[1]", false));
                blockBuilder.Add(new TextCommand($"tp {entity} {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z}", false));

                if (left.Type == TypeSymbol.Float)
                    blockBuilder.Add(GetFloatConversion(functionBuilder, symbol, symbol));

            }
            else if (kind == BoundBinaryOperatorKind.Multiplication)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {_returnValue.SaveName}[-1] set from entity {entity} transformation.translation[0]", false));
                blockBuilder.Add(new TextCommand($"data modify entity {entity} transformation set from storage {MainStorage} {_returnValue.SaveName}", false));
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set from entity {entity} transformation.translation[0]", false));
                functionBuilder.Scope.Declare(_returnValue);

                if (left.Type == TypeSymbol.Double)
                    blockBuilder.Add(GetDoubleConversion(functionBuilder, symbol, symbol));
            }
            else if (kind == BoundBinaryOperatorKind.Division)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set from entity {entity} transformation.translation[0]", false));

                if (left.Type == TypeSymbol.Double)
                    blockBuilder.Add(GetDoubleConversion(functionBuilder, symbol, symbol));
            }

            return new TextBlockEmittionNode(blockBuilder.ToImmutable());
        }

        private TextEmittionNode GetCallExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundCallExpression call, int current)
        {
            //The return value via a temp variable also works for int and bool, but since
            //Mojang's added /return why not use it instead

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

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
                var tempAssignment = GetAssignment(functionBuilder, temp, conversion.Expression, current);

                if (resultType == TypeSymbol.String)
                {
                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        var temp2 = Temp(functionBuilder, TypeSymbol.String, current + 1);

                        return Block(
                                tempAssignment,
                                GetAssignment(functionBuilder, temp2, temp),
                                new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set string storage {MainStorage} {temp2.SaveName}", false)
                            );
                    }
                    else if (sourceType is EnumSymbol enumSymbol)
                    {
                        var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
                        builder.Add(tempAssignment);

                        if (enumSymbol.IsIntEnum)
                        {
                            foreach (var enumMember in enumSymbol.Members)
                            {
                                var intMember = (IntEnumMemberSymbol) enumMember;
                                var command = new TextCommand($"execute if score {temp.SaveName} {Vars} matches {intMember.UnderlyingValue} run data modify storage {MainStorage} {symbol.SaveName} set value \"{enumMember.Name}\"", false);
                                builder.Add(command);
                            }
                            return new TextBlockEmittionNode(builder.ToImmutable());
                        }
                        else
                        {
                            return Block(
                                    tempAssignment,
                                    GetAssignment(functionBuilder, symbol, temp)
                                );
                        }
                    }
                    else if (sourceType == TypeSymbol.Float || sourceType == TypeSymbol.Double || sourceType == TypeSymbol.Object)
                    {
                        return Block(
                            tempAssignment,
                            new TextCommand($"data modify storage {MainStorage} {symbol.SaveName} set string storage {MainStorage} {temp.SaveName}", false)
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
                            tempAssignment,
                            GetFloatConversion(functionBuilder, symbol, temp)
                        );
                }
                if (resultType == TypeSymbol.Double)
                {
                    return Block(
                            tempAssignment,
                            GetDoubleConversion(functionBuilder, symbol, temp)
                        );
                }
                else
                {
                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        return Block(
                            tempAssignment,
                            GetAssignment(functionBuilder, symbol, temp)
                        );
                    }
                    else
                    {
                        return Block(
                            tempAssignment,
                            GetAssignment(functionBuilder, symbol, temp)
                        );
                    }
                }
            }
        }
        private TextEmittionNode GetFloatConversion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToFloat, out var macro))
            {
                macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value $(a)f");
            }

            var macroA = Macro(functionBuilder, "a");

            return Block(
                    GetAssignment(functionBuilder, macroA, right),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, left, _returnValue)
                );
        }

        private TextEmittionNode GetDoubleConversion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            if (GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToDouble, out var macro))
            {
                macro.AddMacro($"data modify storage {MainStorage} {_returnValue.SaveName} set value $(a)d");
            }   

            var macroA = Macro(functionBuilder, "a");
           
            return Block(
                    GetAssignment(functionBuilder, macroA, right),
                    GetMacroCall(macro),
                    GetAssignment(functionBuilder, left, _returnValue)
                );
        }

        private EmittionVariableSymbol Temp(MinecraftFunction.Builder functionBuilder, TypeSymbol type, int tempIndex, string tempName = "temp", EmittionVariableLocation? location = null)
        {
            var name = $"*{tempName}{tempIndex}";
            return functionBuilder.Scope.LookupOrDeclare(name, type, true, false, location);
        }

        private EmittionVariableSymbol Macro(MinecraftFunction.Builder functionBuilder, string subName)
        {
            var macro = new EmittionVariableSymbol($"{_macro.Name}.{subName}", _macro.Type, true, null, _macro.Location);
            functionBuilder.Scope.Declare(_macro);
            return macro;
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

        public bool TryEmitBuiltInFieldGetter(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundFieldAccessExpression right, int current, out TextEmittionNode? node)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(right.Field))
            {
                if (symbol.Location == EmittionVariableLocation.Scoreboard)
                    node = new TextCommand($"execute store result score {symbol.SaveName} {Vars} run gamerule {right.Field.Name}", false);
                else
                    node = new TextCommand($"execute store result storage {MainStorage} {symbol.SaveName} int 1 run gamerule {right.Field.Name}", false);

                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == right.Field)
            {
                if (symbol.Location == EmittionVariableLocation.Scoreboard)
                    node = new TextCommand($"execute store result score {symbol.SaveName} {Vars} run difficulty", false);
                else
                    node = new TextCommand($"execute store result storage {MainStorage} {symbol.SaveName} int 1 run difficulty", false);

                return true;
            }
            node = null;
            return false;
        }

        public bool TryEmitBuiltInFieldAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int current, out TextEmittionNode? node)
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

        private TextEmittionNode GetGameruleAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int tempIndex)
        {
            //1. Evaluate right to temp
            //2. If is bool, generate two conditions
            //3. If it's an int, generate a macro

            var temp = Temp(functionBuilder, right.Type, tempIndex);

            if (field.Type == TypeSymbol.Bool)
            {
                return Block(
                        GetAssignment(functionBuilder, temp, right, tempIndex),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run gamerule {field.Name} true", false),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run gamerule {field.Name} false", false)
                    );
            }
            else
            {
                var macroRule = Macro(functionBuilder, "rule");
                var macroValue = Macro(functionBuilder, "value");

                var macroFunctionSymbol = BuiltInNamespace.Minecraft.General.Gamerules.SetGamerule;

                if (GetOrCreateBuiltIn(macroFunctionSymbol, out var macro))
                {
                    macro.AddMacro("gamerule $(rule) $(value)");
                }

                return Block(
                        GetAssignment(functionBuilder, temp, right, tempIndex),
                        GetAssignment(macroRule, $"\"{field.Name}\""),
                        GetAssignment(functionBuilder, macroValue, temp),
                        GetMacroCall(macro)
                    );
            }
        }

        private TextEmittionNode GetDifficultyAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int tempIndex)
        {
            if (right is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
            {
                return new TextCommand($"difficulty {em.Name.ToLower()}", false);
            }

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var rightSymbol = Temp(functionBuilder, right.Type, tempIndex);
            builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex));

            foreach (var enumMember in BuiltInNamespace.Minecraft.General.Difficulty.Members)
            {
                var intMember = (IntEnumMemberSymbol)enumMember;
                builder.Add(new TextCommand($"execute if score {rightSymbol.SaveName} {Vars} matches {intMember.UnderlyingValue} run difficulty {enumMember.Name.ToLower()}", false));
            }
            
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        public bool TryGetBuiltInFunctionEmittion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol? symbol, BoundCallExpression call, int tempIndex, out TextEmittionNode? node)
        {
            if (call.Function == BuiltInNamespace.Minecraft.General.RunCommand)
            {
                node = EmitRunCommand(functionBuilder, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackEnable)
            {
                node = EmitDatapackEnable(functionBuilder, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackDisable)
            {
                node = EmitDatapackDisable(functionBuilder, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.SetDatapackEnabled)
            {
                node = EmitSetDatapackEnabled(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeather)
            {
                node = EmitSetWeather(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForTicks)
            {
                node = EmitSetWeatherForTicks(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForDays)
            {
                node = EmitSetWeatherForDays(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForSeconds)
            {
                node = EmitSetWeatherForSeconds(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Say)
            {
                node = EmitPrint(functionBuilder, call, tempIndex);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Print)
            {
                node = EmitPrint(functionBuilder, call, tempIndex);
                return true;
            }

            if (symbol == null)
            {
                node = null;
                return false;
            }
                
            if (call.Function == BuiltInNamespace.Minecraft.General.GetDatapackCount)
            {
                node = EmitGetDatapackCount(functionBuilder, symbol, call);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetEnabledDatapackCount)
            {
                node = EmitGetDatapackCount(functionBuilder, symbol, call, true);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetAvailableDatapackCount)
            {
                node = EmitGetDatapackCount(functionBuilder, symbol, call, false, true);
                return true;
            }

            node = null;
            return false;
        }

        private TextEmittionNode EmitRunCommand(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macroCommand = Macro(functionBuilder, "command");
            
            if (GetOrCreateBuiltIn(call.Function, out var macro))
            {
                macro.AddMacro("$(command)");
            }
        
            return Block(
                    GetAssignment(functionBuilder, macroCommand, call.Arguments.First(), 0),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode EmitDatapackEnable(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macrosPack = Macro(functionBuilder, "pack");

            if (GetOrCreateBuiltIn(call.Function, out var macro))
            {
                macro.AddMacro($"datapack enable \"file/$(pack)\"");
            }

            return Block(
                    GetAssignment(functionBuilder, macrosPack, call.Arguments.First()),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode EmitDatapackDisable(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macrosPack = Macro(functionBuilder, "pack");
         
            if (GetOrCreateBuiltIn(call.Function, out var macro))
            {
                macro.AddMacro($"datapack disable \"file/$(pack)\"");
            }

            return Block(
                    GetAssignment(functionBuilder, macrosPack, call.Arguments.First()),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode EmitSetDatapackEnabled(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex)
        {
            var pack = call.Arguments[0];
            var value = call.Arguments[1];

            var macrosPack = Macro(functionBuilder, "pack");
            var temp = new EmittionVariableSymbol("*de", TypeSymbol.Bool, true);
            functionBuilder.Scope.Declare(temp);

            if (GetOrCreateBuiltIn(call.Function, out var macro))
            {
                macro.AddMacro($"execute if score {temp.SaveName} {Vars} matches 1 run return run datapack enable \"file/$(pack)\"");
                macro.AddMacro($"datapack disable \"file/$(pack)\"");
            }

            return Block(
                    GetAssignment(functionBuilder, macrosPack, pack, tempIndex),
                    GetAssignment(functionBuilder, temp, value, tempIndex),
                    GetMacroCall(macro)
                );
        }

        private TextEmittionNode EmitGetDatapackCount(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundCallExpression call, bool countEnabled = false, bool countAvailable = false)
        {
            var filter = countEnabled ? "enabled" : "available";
            return new TextCommand($"execute store result score {symbol.SaveName} {Vars} run datapack list {filter}", false);
        }

        private TextEmittionNode EmitSetWeather(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex, string? timeUnits = null)
        {
            TextEmittionNode EmitNonMacroNonConstantTypeCheck(BoundExpression weatherType, int current, int time = 0, string? timeUnits = null)
            {
                var temp = Temp(functionBuilder, weatherType.Type, current, "type");
                var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

                foreach (var enumMember in BuiltInNamespace.Minecraft.General.Weather.Weather.Members)
                {
                    var intMember = (IntEnumMemberSymbol)enumMember;
                    
                    if (timeUnits == null)
                        builder.Add(new TextCommand($"execute if score {temp.SaveName} {Vars} matches {intMember.UnderlyingValue} run weather {enumMember.Name.ToLower()}", false));
                    else
                        builder.Add(new TextCommand($"execute if score {temp.SaveName} {Vars} matches {intMember.UnderlyingValue} run weather {enumMember.Name.ToLower()} {time}{timeUnits}", false));
                }

                return new TextBlockEmittionNode(builder.ToImmutable());
            }

            var weatherType = call.Arguments[0];

            if (call.Arguments.Length > 1)
            {
                if (call.Arguments[1] is BoundLiteralExpression l)
                {
                    if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                    {
                        return new TextCommand($"weather {em.Name.ToLower()} {l.Value}{timeUnits}", false);
                    }
                    else
                    {
                        var time = (int)l.Value;
                        return EmitNonMacroNonConstantTypeCheck(weatherType, tempIndex, time, timeUnits);
                    }
                }
                else
                {
                    var macroType = Macro(functionBuilder, "type");
                    var duration = Temp(functionBuilder, call.Arguments[1].Type, tempIndex);
                    var macroDuration = Macro(functionBuilder, "duration");
                    var macroTimeUnits = Macro(functionBuilder, "timeUnits");
 
                    if (GetOrCreateBuiltIn(BuiltInNamespace.Minecraft.General.Weather.SetWeather, out var macro))
                    {
                        macro.AddMacro($"weather $(type) $(duration)(timeUnits)");
                    }
   
                    return Block(
                            GetAssignment(functionBuilder, macroType, weatherType, tempIndex),
                            GetAssignment(functionBuilder, duration, call.Arguments[1], tempIndex),
                            GetAssignment(functionBuilder, macroDuration, duration),
                            GetAssignment(macroTimeUnits, $"\"{timeUnits}\""),
                            GetMacroCall(macro)
                        );
                }
            }
            else
            {
                if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                {
                    return new TextCommand($"weather {em.Name.ToLower()}", false);
                }
                else
                {
                    return EmitNonMacroNonConstantTypeCheck(weatherType, tempIndex);
                }
            }
        }

        private TextEmittionNode EmitSetWeatherForTicks(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int current) => EmitSetWeather(functionBuilder, call, current, "t");
        private TextEmittionNode EmitSetWeatherForSeconds(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int current) => EmitSetWeather(functionBuilder, call, current, "s");
        private TextEmittionNode EmitSetWeatherForDays(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int current) => EmitSetWeather(functionBuilder, call, current, "d");

        private TextEmittionNode EmitPrint(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex)
        {
            var argument = call.Arguments[0];
            var command = string.Empty;

            if (argument is BoundLiteralExpression literal)
            {
                return new TextCommand($"tellraw @a {{\"text\":\"{literal.Value}\"}}", false);
            }
            else if (argument is BoundVariableExpression variable)
            {
                var varSymbol = ToEmittionVariable(functionBuilder, variable.Variable, false, true);

                return new TextCommand($"tellraw @a {{\"storage\":\"{MainStorage}\",\"nbt\":\"{varSymbol.SaveName}\"}}", false);
            }
            else
            {
                var temp = Temp(functionBuilder, argument.Type, 0);

                return Block(
                        GetAssignment(functionBuilder, temp, argument, tempIndex),
                        new TextCommand($"tellraw @a {{\"storage\":\"{MainStorage}\",\"nbt\":\"{temp.SaveName}\"}}", false)
                    );
            }
        }
    }
}
