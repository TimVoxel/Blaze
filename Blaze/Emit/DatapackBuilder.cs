using Blaze.Binding;
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
        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;
        private readonly EmittionNameTranslator _nameTranslator;

        private readonly MinecraftFunction.Builder _initFunction;
        private readonly MinecraftFunction.Builder _tickFunction;

        private readonly List<FunctionSymbol> _fabricatedMacroFunctions = new List<FunctionSymbol>();
        private readonly Dictionary<FunctionSymbol, MinecraftFunction.Builder> _usedBuiltIn = new Dictionary<FunctionSymbol, MinecraftFunction.Builder>();
        private readonly Dictionary<string, EmittionVariableSymbol> _variables = new Dictionary<string, EmittionVariableSymbol>();

        private readonly EmittionVariableSymbol _macro = new EmittionVariableSymbol("*macros", TypeSymbol.Object);
        private readonly EmittionVariableSymbol _returnValue = new EmittionVariableSymbol("*return.value", TypeSymbol.Object);

        private EmittionVariableSymbol? _thisSymbol = null;

        private string Vars => _nameTranslator.Vars;
        private string Const => _nameTranslator.Const;
        private string DEBUG_CHUNK_X => EmittionNameTranslator.DEBUG_CHUNK_X;
        private string DEBUG_CHUNK_Z => EmittionNameTranslator.DEBUG_CHUNK_Z;

        public DatapackBuilder(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _configuration = configuration;
            _nameTranslator = new EmittionNameTranslator(configuration.RootNamespace);

            _initFunction = MinecraftFunction.Init(program.GlobalNamespace);
            _tickFunction = MinecraftFunction.Tick(program.GlobalNamespace);
            AddInitializationCommands();
        }

        private TextEmittionNode LineBreak() => TextTriviaNode.LineBreak();
        private TextEmittionNode Comment(string comment) => TextTriviaNode.Comment(comment);

        public Datapack BuildDatapack()
        {
            var functionNamespaceEmittionBuilder = ImmutableArray.CreateBuilder<NamespaceEmittionNode>();

            foreach (var ns in _program.Namespaces)
            {
                var namespaceSymbol = ns.Key;
                var boundNamespace = ns.Value;

                var namespaceEmittion = GetNamespace(namespaceSymbol, boundNamespace);
                functionNamespaceEmittionBuilder.Add(namespaceEmittion);
            }

            foreach (var ns in _program.GlobalNamespace.NestedNamespaces)
            {
                if (ns.IsBuiltIn)
                {
                    var emittion = GetBuiltInNamespace(ns);
                    if (emittion != null)
                        functionNamespaceEmittionBuilder.Add(emittion);
                }
            }

            var initFunction = _initFunction.ToFunction();
            var tickFunction = _tickFunction.ToFunction();
            var datapack = new Datapack(functionNamespaceEmittionBuilder.ToImmutable(), _configuration, _program.Diagnostics, initFunction, tickFunction);
            return datapack;
        }

        private void AddInitializationCommands()
        {
            _initFunction.AddComment("Blaze setup");
            _initFunction.AddCommand(ScoreboardCommand.AddObjective(Vars, "dummy"));
            _initFunction.AddCommand(ScoreboardCommand.AddObjective(Const, "dummy"));
            _initFunction.AddCommand($"scoreboard players set *-1 {Const} -1");

            //Debug chunk setup
            _initFunction.AddLineBreak();
            _initFunction.AddCommand($"forceload add {DEBUG_CHUNK_X} {DEBUG_CHUNK_Z}");
            _initFunction.AddCommand($"kill @e[tag=debug,tag=blz]");
            _initFunction.AddCommand($"summon item_display {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z} {{Tags:[\"blz\",\"debug\", \"first\"], UUID:{_nameTranslator.MathEntity1.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:1,less:0}}}}}}}}");
            _initFunction.AddCommand($"summon item_display {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z} {{Tags:[\"blz\",\"debug\", \"second\"], UUID:{_nameTranslator.MathEntity2.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:0,less:1}}}}}}}}");
        }


        private EmittionVariableSymbol ToEmittionVariable(VariableSymbol variable)
        {
            if (!_variables.TryGetValue(variable.Name, out var emittionVariable))
            {
                EmittionVariableLocation? location = null;

                if (variable.Type is ArrayTypeSymbol)
                    location = EmittionVariableLocation.Storage;

                emittionVariable = new EmittionVariableSymbol(variable.Name, variable.Type, location);
                _variables.Add(emittionVariable.Name, emittionVariable);
                return emittionVariable;
            }
            return emittionVariable;
        }

        private EmittionVariableSymbol ToEmittionVariable(MinecraftFunction.Builder functionBuilder, BoundExpression left, out TextEmittionNode? setupNode, int tempIndex)
        {
            setupNode = null;

            if (left is BoundVariableExpression v)
                return ToEmittionVariable(v.Variable);

            var leftAssociativeOrder = new Stack<BoundExpression>();
            leftAssociativeOrder.Push(left);

            while (true)
            {
                var current = leftAssociativeOrder.Peek();

                if (current is BoundFieldAccessExpression fieldAccess)
                    leftAssociativeOrder.Push(fieldAccess.Instance);
                else if (current is BoundCallExpression call)
                    leftAssociativeOrder.Push(call.Identifier);
                else if (current is BoundMethodAccessExpression methodAccess)
                    leftAssociativeOrder.Push(methodAccess.Instance);
                else if (current is BoundArrayAccessExpression arrayAccess)
                    leftAssociativeOrder.Push(arrayAccess.Identifier);
                else
                    break;
            }

            //We use '.' in between the names
            //Storages have built-in functionality for nesting
            //So every accessor is automatically directed to a storage

            var nameBuilder = new StringBuilder();
            EmittionVariableLocation? location = null;
            ImmutableArray<TextEmittionNode>.Builder? setupBuilder = null;

            while (leftAssociativeOrder.Any())
            {
                var current = leftAssociativeOrder.Pop();

                //Some of the stuff we added might be side products
                //like BoundMethodAccessExpression, that can be just skipped

                if (current is BoundVariableExpression variableExpression)
                {
                    nameBuilder.Append(variableExpression.Variable.Name);
                }
                else if (current is BoundThisExpression thisExpression)
                {
                    Debug.Assert(_thisSymbol != null);

                    location = EmittionVariableLocation.Storage;
                    nameBuilder.Append(_thisSymbol.Name);
                }
                else if (current is BoundNamespaceExpression namespaceExpression)
                {
                    nameBuilder.Append(namespaceExpression.Namespace.Name);
                }
                else if (current is BoundFieldAccessExpression fieldAccess)
                {
                    location = EmittionVariableLocation.Storage;
                    nameBuilder.Append($".{fieldAccess.Field.Name}");
                }
                else if (current is BoundArrayAccessExpression arrayAccess)
                {
                    //if (tempIndex == null || emittion == null)
                    //    throw new Exception("Array access outside of a function");

                    //TODO: Allow non-constant array access in a more civilised implementation
                    location = EmittionVariableLocation.Storage;

                    foreach (var argument in arrayAccess.Arguments)
                    {
                        if (argument.ConstantValue == null)
                            throw new Exception("Non-consant array access  is not supported with the current implementation");

                        nameBuilder.Append($"[{argument.ConstantValue.Value}]");
                    }
                }
                else if (current is BoundCallExpression call)
                {
                    if (setupBuilder == null)
                        setupBuilder = ImmutableArray.CreateBuilder<TextEmittionNode>();

                    location = EmittionVariableLocation.Storage;

                    var callEmittion = GetCallExpression(functionBuilder, null, call, tempIndex);
                    setupBuilder.Add(callEmittion);
                    
                    nameBuilder.Clear();
                    nameBuilder.Append(_returnValue.Name);
                }
                else if (current is BoundMethodAccessExpression || current is BoundFunctionExpression)
                {
                    continue;
                }
                else 
                {
                    throw new Exception($"Unexpected expression kind {current.Kind}");
                }
            }

            if (setupBuilder != null)
                setupNode = new TextBlockEmittionNode(setupBuilder.ToImmutable());

            var name = nameBuilder.ToString();

            if (!_variables.TryGetValue(name, out var emittionVariable))
            {
                emittionVariable = new EmittionVariableSymbol(name, left.Type, location);
                _variables.Add(name, emittionVariable);
            }
            return emittionVariable;
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
                result = new NamespaceEmittionNode(ns, ns.Name, children);
                result.Children.AddRange();
            }
            return result;
        }

        private NamespaceEmittionNode GetNamespace(NamespaceSymbol symbol, BoundNamespace boundNamespace)
        {
            var childrenBuilder = ImmutableArray.CreateBuilder<StructureEmittionNode>();
            var ns = new BoundNamespaceExpression(symbol);

            foreach (var field in symbol.Fields)
            {
                if (field.Initializer == null)
                    continue;

                var fieldAccess = new BoundFieldAccessExpression(ns, field);
                var name = ToEmittionVariable(_initFunction, fieldAccess, out var setupNode, 0);

                if (setupNode != null)
                    _initFunction.Content.Add(setupNode);

                _initFunction.Content.Add(GetAssignment(_initFunction, name, field.Initializer, 0));
            }

            foreach (var function in boundNamespace.Functions)
            {
                var functionEmittion = GetFunction(function.Key, function.Value);
                childrenBuilder.Add(functionEmittion);
            }

            foreach (var child in boundNamespace.Children)
            {
                var nestedNamespace = GetNamespace(child.Key, child.Value);
                childrenBuilder.Add(nestedNamespace);
            }

            var namespaceEmittion = new NamespaceEmittionNode(symbol, symbol.Name, childrenBuilder.ToImmutable());
            return namespaceEmittion;
        }

        private MinecraftFunction GetFunction(FunctionSymbol function, BoundStatement bodyBlock)
        {
            var functionBuilder = new MinecraftFunction.Builder(function.Name, function, null);

            if (function.IsLoad)
                _initFunction.AddCommand($"function {_nameTranslator.GetCallLink(function)}");

            if (function.IsTick)
                _tickFunction.AddCommand($"function {_nameTranslator.GetCallLink(function)}");

            if (function.ReturnType is NamedTypeSymbol)
                functionBuilder.Content.Add(Block(
                        Comment("This function returns a named type so we clear the return value first"),
                        new TextCommand($"data remove storage {_nameTranslator.MainStorage} {_returnValue.SaveName}", false)
                    ));

            var body = GetStatement(functionBuilder, bodyBlock);
            functionBuilder.Content.Add(body);

            if (functionBuilder.Locals.Any() && functionBuilder.Content.FirstOrDefault(n => n.IsCleanUp) == null)
                functionBuilder.Content.Add(GetCleanUp(functionBuilder));

            return functionBuilder.ToFunction();
        }

        private TextEmittionNode GetCleanUp(MinecraftFunction.Builder functionBuilder)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            builder.Add(Comment("Clean up"));

            foreach (var local in functionBuilder.Locals)
            {
                switch (local.Location)
                {
                    case EmittionVariableLocation.Scoreboard:
                        builder.Add(new TextCommand($"scoreboard players reset {local.SaveName} {Vars}", true));
                        break;
                    case EmittionVariableLocation.Storage:
                        builder.Add(new TextCommand($"data remove storage {_nameTranslator.MainStorage} {local.SaveName}", true));
                        break;
                    default:
                        throw new Exception($"Unexpected variable location {local.Location}");
                }
            }
            return new TextBlockEmittionNode(builder.ToImmutable(), true);
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

            foreach (var statement in node.Statements)
                builder.Add(GetStatement(functionBuilder, statement));

            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private TextEmittionNode GetVariableDeclarationStatement(MinecraftFunction.Builder functionBuilder, BoundVariableDeclarationStatement node)
        {
            var variable = ToEmittionVariable(node.Variable);
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
            var textBody = GetStatement(subFunction, node.Body);
            subFunction.Content.Add(textBody);

            var temp = Temp(TypeSymbol.Bool, 0);
            functionBuilder.AddLocal(temp);
            var tempAssignment = GetAssignment(functionBuilder, temp, node.Condition, 0);

            functionBuilder.AddLocal(temp);

            if (node.ElseBody == null)
                return Block(
                        tempAssignment,
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run function {_nameTranslator.GetCallLink(subFunction)}", false)
                    );
            else
            {
                var elseSubFunction = functionBuilder.CreateSub(SubFunctionKind.Else);
                elseSubFunction.Content.Add(GetStatement(elseSubFunction, node.ElseBody));

                return Block(
                        tempAssignment,
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run function {_nameTranslator.GetCallLink(subFunction)}", false),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run function {_nameTranslator.GetCallLink(elseSubFunction)}", false)
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
            var temp = Temp(TypeSymbol.Bool, 0);
            functionBuilder.AddLocal(temp);

            var callCommand = new TextCommand($"function {_nameTranslator.GetCallLink(subFunction)}", false);

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
            var temp = Temp(TypeSymbol.Bool, 0);
            functionBuilder.AddLocal(temp);

            var callCommand = new TextCommand($"function {_nameTranslator.GetCallLink(subFunction)}", false);

            subFunction.Content.Add(GetAssignment(subFunction, temp, node.Condition, 0));
            subFunction.AddLineBreak();
            subFunction.Content.Add(GetStatement(subFunction, node.Body));
            subFunction.Content.Add(new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run function {_nameTranslator.GetCallLink(subFunction)}", false));
            
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
            if (returnExpression == null)
            {
                return Block(
                        GetCleanUp(functionBuilder),
                        new TextCommand("return 0", false)
                    );
            }

            return Block(
                    GetCleanUp(functionBuilder),
                    GetAssignment(functionBuilder, _returnValue, returnExpression, 0),
                    new TextCommand($"return run data get storage {_nameTranslator.MainStorage} {_returnValue.SaveName} 1", false)
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

            var node = TryGetBuiltInFunctionEmittion(functionBuilder, symbol, call, current);

            if (node != null)
                return node;

            return Block(
                        GetFunctionParametersAssignment(functionBuilder, call.Function.Parameters, call.Arguments),
                        new TextCommand($"function {_nameTranslator.GetCallLink(call.Function)}", false)
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
            //Built-in fields may require non-scoreboard approachers
            //Such fields include gamerules, difficulty and other things
            //Entity and block data management requires the /data command

            if (left is BoundFieldAccessExpression fieldAccess)
            {
                var node = TryEmitBuiltInFieldAssignment(functionBuilder, fieldAccess.Field, right, tempIndex);

                if (node != null)
                    return node;
            }

            var leftVariable = ToEmittionVariable(functionBuilder, left, out var setupNode, tempIndex);

            return Block(
                    setupNode ?? TextTriviaNode.Empty(),
                    GetAssignment(functionBuilder, leftVariable, right, tempIndex)
                );
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
                var leftVariable = ToEmittionVariable(functionBuilder, a.Left, out var setupNode, tempIndex);
                
                return Block(
                        setupNode ?? TextTriviaNode.Empty(),
                        GetAssignment(functionBuilder, leftVariable, a.Right, tempIndex),
                        GetVariableAssignment(functionBuilder, symbol, leftVariable)
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
            else if (expression is BoundFieldAccessExpression fieldExpression)
            {
                var node = TryEmitBuiltInFieldGetter(functionBuilder, symbol, fieldExpression, tempIndex);

                if (node != null)
                    return node;

                var rightSymbol = ToEmittionVariable(functionBuilder, fieldExpression, out var setupNode, tempIndex);

                emittionNode = Block(
                        setupNode ?? TextTriviaNode.Empty(), 
                        GetVariableAssignment(functionBuilder, symbol, rightSymbol)
                    );
            }
            else if (expression is BoundArrayAccessExpression arrayAccessExpression)
            {
                emittionNode = GetArrayAccessAssignment(functionBuilder, symbol, arrayAccessExpression, tempIndex);
            }
            else
            {
                throw new Exception($"Unexpected expression kind {expression.Kind}");
            }
            return emittionNode;
        }

        private TextEmittionNode GetLiteralAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundLiteralExpression literal)
        {
            //Storage symbol    -> data modify storage main <name> set value <value>
            //Scoreboard symbol -> scoreboard players set <name> <vars> <value>

            if (literal.Type == TypeSymbol.Int)
            {
                var value = (int)literal.Value;

                if (symbol.Location == EmittionVariableLocation.Scoreboard)
                    return new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} {value}", false);
                else
                    return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value {value}", false);
            }
            else if (literal.Type == TypeSymbol.Bool)  
            {
                var value = ((bool)literal.Value) ? 1 : 0;

                if (symbol.Location == EmittionVariableLocation.Scoreboard)
                    return new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} {value}", false);
                else
                    return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value {value}", false);
            }
            else if (literal.Type == TypeSymbol.String)
            {
                var value = (string)literal.Value;
                value = value.Replace("\"", "\\\"");
                return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value \"{value}\"", false);
            }
            else if (literal.Type == TypeSymbol.Float)
            {
                var value = (float)literal.Value;
                return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value {value}f", false);
            }
            else if (literal.Type == TypeSymbol.Double)
            {
                var value = (double)literal.Value;
                return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value {value}d", false);
            }
            else
            {
                throw new Exception($"Unexpected storage stored literal type {literal.Type}");
            }
        }

        private TextEmittionNode GetVariableAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundVariableExpression variable)
        {
            if (variable.Variable is StringEnumMemberSymbol stringEnumMember)
            {
                var value = stringEnumMember.UnderlyingValue;
                return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value \"{value}\"", false);
            }
            else if (variable.Variable is IntEnumMemberSymbol intEnumMember)
            {
                var value = intEnumMember.UnderlyingValue;
                return new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} {value}", false);
            }
            else
            {
                var rightVariable = ToEmittionVariable(variable.Variable);
                return GetVariableAssignment(functionBuilder, symbol, rightVariable);
            }
        }

        private TextEmittionNode GetVariableAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            //TODO: return empty trivia when two readonly variables are being assigned

            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string, float, double literal    -> data modify storage strings *this set from storage strings *other
            //named type        -> assign all the fields to the corresponding ones of the object we are copying

            if (left == right)
                return TextTriviaNode.Empty();

            if (left.Location == EmittionVariableLocation.Storage && right.Location == EmittionVariableLocation.Storage)
            {
                return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {left.SaveName} set from storage {_nameTranslator.MainStorage} {right.SaveName}", false);
            }

            if (left.Location == EmittionVariableLocation.Scoreboard && right.Location == EmittionVariableLocation.Scoreboard)
            {
                return new TextCommand($"scoreboard players operation {left.SaveName} {Vars} = {right.SaveName} {Vars}", false);
            }

            if (left.Location == EmittionVariableLocation.Storage && right.Location == EmittionVariableLocation.Scoreboard)
            {
                return new TextCommand($"execute store result storage {_nameTranslator.MainStorage} {left.SaveName} int 1 run scoreboard players get {right.SaveName} {Vars}", false);
            }

            if (left.Location == EmittionVariableLocation.Scoreboard && right.Location == EmittionVariableLocation.Storage)
            {
                return new TextCommand($"execute store result score {left.SaveName} {Vars} run data get storage {_nameTranslator.MainStorage} {right.SaveName}", false);
            }

            throw new Exception($"Unexpected variable location combination {left.Location} and {right.Location}");
        }

        private TextEmittionNode GetArrayAccessAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundArrayAccessExpression arrayAccessExpression, int tempIndex)
        {
            //if all the arguments have constant values, just add [a][b][c]... to the accessed name
            //Otherwise, create a macro (or get an existing one) for the corresponding rank of the array
            //Set its arguments (and the name to access) to the corresponding values and copy the return value to the desired location

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            builder.Add(Comment($"Emitting array access to \"{symbol.SaveName}\", stored in variable \"*array\"\r\n"));

            var rank = arrayAccessExpression.Arguments.Length;
            var macroFunctionName = $"array_access_rank{rank}";
            var accessed = ToEmittionVariable(functionBuilder, arrayAccessExpression.Identifier, out var setupNode, tempIndex);

            if (setupNode != null)
                builder.Add(setupNode);

            var fabricatedAccessor = _fabricatedMacroFunctions.FirstOrDefault(f => f.Name == macroFunctionName);
            
            if (fabricatedAccessor == null)
            {
                fabricatedAccessor = new FunctionSymbol(macroFunctionName, BuiltInNamespace.Blaze.Fabricated.Symbol, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Object, false, false, AccessModifier.Private, null);
                BuiltInNamespace.Blaze.Fabricated.Symbol.Members.Add(fabricatedAccessor);
                _fabricatedMacroFunctions.Add(fabricatedAccessor);
            }

            var accessorEmittion = GetOrCreateBuiltIn(fabricatedAccessor, out bool isCreated);

            //var command1 = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.rule set value \"{field.Name}\"";
            //var command2 = $"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.value int 1 run scoreboard players get {rightName} {Vars}";
            //var command3 = ";

            //if (isCreated)
            //    macro.AppendMacro("gamerule $(rule) $(value)");

            if (isCreated)
            {
                var accessRankBuilder = new StringBuilder();

                for (int i = 0; i < rank; i++)
                    accessRankBuilder.Append($"[$({"a" + i.ToString()})]");

                accessorEmittion.AddMacro($"data modify storage {_nameTranslator.MainStorage} {_returnValue.SaveName} set from storage {_nameTranslator.MainStorage} $(name){accessRankBuilder.ToString()}");
            }

            var macroName = Macro("name");
            functionBuilder.AddLocal(_macro);
            builder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroName.SaveName} set value \"{accessed.SaveName}\"", false));

            for (int i = 0; i < rank; i++)
            {
                var argumentSymbol = new EmittionVariableSymbol($"{_macro.Name}.a{i.ToString()}", TypeSymbol.Object);

                var temp = Temp(TypeSymbol.Int, tempIndex + i);
                functionBuilder.AddLocal(temp);

                builder.Add(GetAssignment(functionBuilder, temp, arrayAccessExpression.Arguments[i], tempIndex + i));
                builder.Add(new TextCommand($"execute store result storage {_nameTranslator.MainStorage} {argumentSymbol.SaveName} int 1 run scoreboard players get {temp.SaveName} {Vars}", false));
            }

            builder.Add(new TextCommand($"function {_nameTranslator.GetCallLink(accessorEmittion)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false));
            builder.Add(GetVariableAssignment(functionBuilder, symbol, _returnValue));
            builder.Add(LineBreak());
            return new TextBlockEmittionNode(builder.ToImmutable());
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
                    GetFunctionParametersAssignment(functionBuilder, constructor.Parameters, objectCreationExpression.Arguments),
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
            var defaultValue = GetEmittionDefaultValue(arrayCreationExpression.ArrayType.Type);
            EmittionVariableSymbol? previous = null;
            
            builder.Add(Comment($"Emitting array creation of type {arrayCreationExpression.ArrayType}, stored in variable \"{symbol.SaveName}\""));

            for (int i = arrayCreationExpression.Dimensions.Length - 1; i >= 0; i--)
            {
                EmittionVariableSymbol arraySymbol;
                if (i == 0)
                    arraySymbol = symbol;
                else
                {
                    arraySymbol = new EmittionVariableSymbol($"*rank{tempIndex + i}", TypeSymbol.Object);
                }

                TextCommand assignmentCommand;

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
                        assignmentCommand = new TextCommand($"data modify storage {_nameTranslator.MainStorage} {arraySymbol.SaveName} set value {initializerBuilder.ToString()}", false);
                        builder.Add(assignmentCommand);
                        previous = arraySymbol;
                        continue;
                    }
                    else
                        assignmentCommand = new TextCommand($"data modify storage {_nameTranslator.MainStorage} {arraySymbol.SaveName} append value {defaultValue}", false);
                }
                else
                    assignmentCommand = new TextCommand($"data modify storage {_nameTranslator.MainStorage} {arraySymbol.SaveName} append from storage {_nameTranslator.MainStorage} {previous?.SaveName}", false);

                builder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {arraySymbol.SaveName} set value []", false));

                var subFunction = functionBuilder.CreateSub(SubFunctionKind.Loop);
                var callCommand = new TextCommand($"function {_nameTranslator.GetCallLink(subFunction)}", false);
                
                var tempIter = Temp(TypeSymbol.Int, tempIndex + i, "iter");
                var tempUpperBound = Temp(TypeSymbol.Int, tempIndex + i, "upperBound");
                functionBuilder.AddLocal(tempIter);
                functionBuilder.AddLocal(tempUpperBound);

                builder.Add(GetAssignment(functionBuilder, tempIter, new BoundLiteralExpression(0), tempIndex + i));
                builder.Add(GetAssignment(functionBuilder, tempUpperBound, arrayCreationExpression.Dimensions[i], tempIndex + i));
                builder.Add(callCommand);

                if (previous != null)
                    builder.Add(new TextCommand($"data remove storage {_nameTranslator.MainStorage} {previous.SaveName}", false));

                subFunction.AddCommand(assignmentCommand);
                subFunction.AddCommand($"scoreboard players add {tempIter.SaveName} {Vars} 1");
                subFunction.AddCommand($"execute if score {tempIter.SaveName} {Vars} < {tempUpperBound.SaveName} {Vars} run function {_nameTranslator.GetCallLink(subFunction)}");

                previous = arraySymbol;
            }
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        private string GetEmittionDefaultValue(TypeSymbol type)
        {
            if (type is NamedTypeSymbol)
                return "{}";
            if (type is EnumSymbol e)
                return "{}";
            if (type == TypeSymbol.Object)
                return "0";
            else if (type == TypeSymbol.Int)
                return "0";
            else if (type == TypeSymbol.Float)
                return "0.0f";
            else if (type == TypeSymbol.Double)
                return "0.0d";
            else if (type == TypeSymbol.Bool)
                return "0";
            else if (type == TypeSymbol.String)
                return "\"\"";

            return "{}";
        }

        private TextEmittionNode GetUnaryExpressionAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundUnaryExpression unary, int current)
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
                            GetAssignment(functionBuilder, symbol, operand, current)
                        );

                case BoundUnaryOperatorKind.Negation:

                    if (operand.Type == TypeSymbol.Int)
                    {
                        return Block(
                                LineBreak(),
                                Comment($"Emitting integer negation unary expression \"{unary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, symbol, operand, current),
                                new TextCommand($"scoreboard players operation {symbol.SaveName} {Vars} *= *-1 {Const}", false)
                            );
                    }
                    else
                    {
                        var macroA = Macro("a");
                        var macroSign = Macro("sign");
                        var macroReturn = Macro("return");
                        var sign = new EmittionVariableSymbol($"*sign", TypeSymbol.Object);
                        var last = new EmittionVariableSymbol($"*last", TypeSymbol.Object);

                        functionBuilder.AddLocal(_macro);
                        functionBuilder.AddLocal(sign);
                        functionBuilder.AddLocal(last);

                        MinecraftFunction.Builder macro;

                        if (operand.Type == TypeSymbol.Float)
                        {
                            macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateFloat, out bool isCreated);
                            if (isCreated)
                                macro.AddMacro($"data modify storage {_nameTranslator.MainStorage} **macros.return set value $(sign)$(a)f");
                        }
                        else
                        {
                            macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateDouble, out bool isCreated);
                            if (isCreated)
                                macro.AddMacro($"data modify storage {_nameTranslator.MainStorage} **macros.return set value $(sign)$(a)d");
                        }

                        var typeSuffix = operand.Type == TypeSymbol.Float ? "f" : "d";

                        return Block(
                                LineBreak(),
                                Comment($"Emitting floating point negation unary expression \"{unary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, symbol, operand, current),
                                new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set from storage {_nameTranslator.MainStorage} {symbol.SaveName}", false),
                                new TextCommand($"data modify storage {_nameTranslator.MainStorage} {sign.SaveName} set string storage {_nameTranslator.MainStorage} {symbol.SaveName} 0 1", false),
                                new TextCommand($"data modify storage {_nameTranslator.MainStorage} {last.SaveName} set string storage {_nameTranslator.MainStorage} {symbol.SaveName} -1", false),
                                new TextCommand($"execute if data storage {_nameTranslator.MainStorage} {{ {sign.SaveName}: \"-\"}} run data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set string storage {_nameTranslator.MainStorage} {macroA.SaveName} 1", false),
                                new TextCommand($"execute if data storage {_nameTranslator.MainStorage} {{ {last.SaveName}: \"{typeSuffix}\"}} run data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set string storage {_nameTranslator.MainStorage} {macroA.SaveName} 0 -1", false),
                                new TextCommand($"execute if data storage {_nameTranslator.MainStorage} {{ {sign.SaveName}: \"-\"}} run data modify storage {_nameTranslator.MainStorage} {macroSign.SaveName} set value \"\"", false),
                                new TextCommand($"execute unless data storage {_nameTranslator.MainStorage} {{ {sign.SaveName}: \"-\"}} run data modify storage {_nameTranslator.MainStorage} {macroSign.SaveName} set value \"-\"", false),
                                new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false),
                                new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set from storage {_nameTranslator.MainStorage} {macroReturn.SaveName}", false)
                            );
                    }
                case BoundUnaryOperatorKind.LogicalNegation:

                    var temp = Temp(operand.Type, current);
                    functionBuilder.AddLocal(temp);

                    return Block(
                            LineBreak(),
                            Comment($"Emitting logical negation \"{unary}\" to \"{symbol.SaveName}\""),
                            GetAssignment(functionBuilder, temp, operand, current),
                            new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 0", false),
                            new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run scoreboard players set {symbol.SaveName} {Vars} 0", false)
                        );

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
                    else
                    {
                        if (operatorKind == BoundBinaryOperatorKind.Addition)
                            return TextTriviaNode.Comment("String concatenation is currently unsupported");
                        else
                            throw new Exception($"Unexpected operator kind {operatorKind} for types {left.Type} and {right.Type}");
                    }
                case BoundBinaryOperatorKind.LogicalMultiplication:
                    {
                        var leftSymbol = Temp(left.Type, tempIndex + 1, $"lbTemp");
                        var rightSymbol = Temp(right.Type, tempIndex + 1, $"rbTemp");
                        functionBuilder.AddLocal(leftSymbol);
                        functionBuilder.AddLocal(rightSymbol);

                        return Block(
                                LineBreak(),
                                Comment($"Emitting logical multiplication (and) operation \"{binary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} 0", false),
                                new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches 1 if score {rightSymbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
                            );
                    }
                case BoundBinaryOperatorKind.LogicalAddition:
                    {
                        var leftSymbol = Temp(left.Type, tempIndex + 1, $"lbTemp");
                        var rightSymbol = Temp(right.Type, tempIndex + 1, $"rbTemp");
                        functionBuilder.AddLocal(leftSymbol);
                        functionBuilder.AddLocal(rightSymbol);

                        return Block(
                                LineBreak(),
                                Comment($"Emitting logical multiplication (and) operation \"{binary}\" to \"{symbol.SaveName}\""),
                                GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} 0", false),
                                new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 1", false),
                                new TextCommand($"execute if score {rightSymbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
                            );
                    }
                case BoundBinaryOperatorKind.Equals:
                    {
                        if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object || left.Type is EnumSymbol e && !e.IsIntEnum)
                        {
                            var leftSymbol = Temp(left.Type, tempIndex + 1, $"lbTemp");
                            var rightSymbol = Temp(right.Type, tempIndex + 1, $"rbTemp");
                            functionBuilder.AddLocal(leftSymbol);
                            functionBuilder.AddLocal(rightSymbol);

                            return Block(
                                    LineBreak(),
                                    Comment($"Emitting equals operation \"{binary}\" to \"{symbol.SaveName}\""),
                                    GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                    GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                    new TextCommand($"execute store success score {symbol.SaveName} {Vars} run data modify storage {_nameTranslator.MainStorage} {leftSymbol.SaveName} set from storage {_nameTranslator.MainStorage} {rightSymbol.SaveName}", false),
                                    new TextCommand($"execute if score {symbol.SaveName} {Vars} matches 1 run scoreboard players set {symbol.SaveName} {Vars} 0", false),
                                    new TextCommand($"execute if score {symbol.SaveName} {Vars} matches 0 run scoreboard players set {symbol.SaveName} {Vars} 1", false)
                                );
                        }
                        else if (left.Type == TypeSymbol.Float || left.Type == TypeSymbol.Double)
                        {
                            return EmitFloatingPointComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                        }
                        else
                        {
                            return EmitIntComparisonOperation(functionBuilder, left, right, symbol, operatorKind, tempIndex);
                        }
                    }
                case BoundBinaryOperatorKind.NotEquals:
                    {
                        if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object || left.Type is EnumSymbol e && !e.IsIntEnum)
                        {
                            var leftSymbol = Temp(left.Type, tempIndex + 1, $"lbTemp");
                            var rightSymbol = Temp(right.Type, tempIndex + 1, $"rbTemp");
                            functionBuilder.AddLocal(leftSymbol);
                            functionBuilder.AddLocal(rightSymbol);

                            return Block(
                                    LineBreak(),
                                    Comment($"Emitting equals operation \"{binary}\" to \"{symbol.SaveName}\""),
                                    GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1),
                                    GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1),
                                    new TextCommand($"execute store success score {symbol.SaveName} {Vars} run data modify storage {_nameTranslator.MainStorage} {leftSymbol.SaveName} set from storage {_nameTranslator.MainStorage} {rightSymbol.SaveName}", false)
                                );
                        }
                        else
                        {
                            return EmitIntComparisonOperation(functionBuilder, left, right, symbol, operatorKind, tempIndex);
                        }
                    }
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.LessOrEquals:
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    if (left.Type == TypeSymbol.Int)
                    {
                        return EmitIntComparisonOperation(functionBuilder, left, right, symbol, operatorKind, tempIndex);
                    }
                    else
                    {
                        return EmitFloatingPointComparisonOperation(functionBuilder, symbol, left, right, operatorKind, tempIndex);
                    }
                default:
                    throw new Exception($"Unexpected binary operator kind {operatorKind} for types {left.Type} and {right.Type}");
            }
        }

        private TextEmittionNode GetIntBinaryAssignment(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int tempIndex)
        {
            var textNodeBuilder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var leftAssignment = GetAssignment(functionBuilder, symbol, left, tempIndex);
            textNodeBuilder.Add(leftAssignment);

            EmittionVariableSymbol rightSymbol;

            EmittionVariableSymbol AddAssignRightTemp()
            {
                rightSymbol = Temp(right.Type, tempIndex + 1, "rTemp");
                functionBuilder.AddLocal(rightSymbol);
                var rightAssignment = GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1);
                textNodeBuilder.Add(rightAssignment);
                return rightSymbol;
            }

            if (right is BoundLiteralExpression l)
            {
                if (operation == BoundBinaryOperatorKind.Addition)
                {
                    return Block(
                            leftAssignment,
                            new TextCommand($"scoreboard players add {symbol.SaveName} {Vars} {l.Value}", false)
                       );
                }
                else if (operation == BoundBinaryOperatorKind.Subtraction)
                {
                    return Block(
                            leftAssignment,
                            new TextCommand($"scoreboard players remove {symbol.SaveName} {Vars} {l.Value}", false)
                        );
                }
                else
                {
                    AddAssignRightTemp();
                }
            }
            else if (right is BoundVariableExpression v)
            {
                rightSymbol = ToEmittionVariable(v.Variable);
            }
            else
            {
                AddAssignRightTemp();
            }

            var operationSign = operation switch
            {
                BoundBinaryOperatorKind.Addition => "+=",
                BoundBinaryOperatorKind.Subtraction => "-=",
                BoundBinaryOperatorKind.Multiplication => "*=",
                BoundBinaryOperatorKind.Division => "/=",
                _ => "="
            };
            var mainCommand = new TextCommand($"scoreboard players operation {symbol.SaveName} {Vars} {operationSign} {rightSymbol.SaveName} {Vars}", false);
            textNodeBuilder.Add(mainCommand);
            return new TextBlockEmittionNode(textNodeBuilder.ToImmutable());
        }

        private TextEmittionNode EmitIntComparisonOperation(MinecraftFunction.Builder functionBuilder, BoundExpression left, BoundExpression right, EmittionVariableSymbol symbol, BoundBinaryOperatorKind operatorKind, int tempIndex)
        {
            EmittionVariableSymbol leftSymbol;

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
                    var other = (IntEnumMemberSymbol)((BoundVariableExpression)right).Variable;
                    var result = (other.UnderlyingValue == enumMember.UnderlyingValue) ? successValue : initialValue;
                    return new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} {result}", false);
                }

                leftSymbol = ToEmittionVariable(v.Variable);
            }
            else
            {
                leftSymbol = Temp(left.Type, tempIndex + 1, "lTemp");
                functionBuilder.AddLocal(leftSymbol);
                builder.Add(GetAssignment(functionBuilder, leftSymbol, left, tempIndex + 1));
            }

            builder.Add(new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} {initialValue}", false));

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
                EmittionVariableSymbol rightSymbol;

                if (right is BoundVariableExpression vr)
                {
                    if (vr.Variable is IntEnumMemberSymbol enumMember)
                    {
                        var memberUnderlyingValue = enumMember.UnderlyingValue;
                        builder.Add(new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} matches {memberUnderlyingValue} run scoreboard players set {symbol.SaveName} {Vars} {successValue}", false));
                        return new TextBlockEmittionNode(builder.ToImmutable());
                    }

                    rightSymbol = ToEmittionVariable(vr.Variable);
                }
                else
                {
                    rightSymbol = Temp(right.Type, tempIndex + 1, "rTemp");
                    functionBuilder.AddLocal(rightSymbol);
                    builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex + 1));
                }
                var operationSign = operatorKind switch
                {
                    BoundBinaryOperatorKind.Less => "<",
                    BoundBinaryOperatorKind.LessOrEquals => "<=",
                    BoundBinaryOperatorKind.Greater => ">",
                    BoundBinaryOperatorKind.GreaterOrEquals => ">=",
                    _ => "="
                };
                builder.Add(new TextCommand($"execute if score {leftSymbol.SaveName} {Vars} {operationSign} {rightSymbol.SaveName} {Vars} run scoreboard players set {symbol.SaveName} {Vars} {successValue}", false));
            }

            return new TextBlockEmittionNode(builder.ToImmutable());
        }
        private TextEmittionNode EmitFloatingPointComparisonOperation(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int current)
        {
            var stringStorage = _nameTranslator.MainStorage;
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.PositionY, out bool isCreated);
            if (isCreated)
                macro.AddMacro($"tp @s {DEBUG_CHUNK_X} $(a) {DEBUG_CHUNK_Z}");

            var macroA = Macro("a");
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            functionBuilder.AddLocal(_macro);

            var temp = Temp(left.Type, current + 1);
            functionBuilder.AddLocal(temp);
            builder.Add(GetAssignment(functionBuilder, temp, left, current + 1));
            builder.Add(new TextCommand($"data modify storage {stringStorage} {macroA.SaveName} set from storage {stringStorage} {temp.SaveName}", false));
            builder.Add(new TextCommand($"execute as {_nameTranslator.MathEntity1} run function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} {_macro.SaveName}", false));

            temp = Temp(right.Type, current + 1);
            functionBuilder.AddLocal(temp);
            builder.Add(GetAssignment(functionBuilder, temp, right, current + 1));
            builder.Add(new TextCommand($"data modify storage {stringStorage} {macroA.SaveName} set from storage {stringStorage} {temp.SaveName}", false));
            builder.Add(new TextCommand($"execute as {_nameTranslator.MathEntity2} run function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} {_macro.SaveName}", false));

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Equals:
                    builder.Add(new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} 0", false));
                    builder.Add(new TextCommand($"execute as {_nameTranslator.MathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 1", false));
                    break;

                case BoundBinaryOperatorKind.NotEquals:
                    builder.Add(new TextCommand($"scoreboard players set {symbol.SaveName} {Vars} 1", false));
                    builder.Add(new TextCommand($"execute as {_nameTranslator.MathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {symbol.SaveName} {Vars} 0", false));
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

            var rightSymbol = Temp(right.Type, current + 1, "rTemp");
            functionBuilder.AddLocal(rightSymbol);
            var rightAssignment = GetAssignment(functionBuilder, rightSymbol, right, current + 1);
            blockBuilder.Add(rightAssignment);
            
            var macroA = Macro("a");
            var macroB = Macro("b");
            var macroPolarity = Macro("polarity");
            functionBuilder.AddLocal(_macro);

            if (kind != BoundBinaryOperatorKind.Subtraction)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set from storage {_nameTranslator.MainStorage} {symbol.SaveName}", false));
                blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroB.SaveName} set from storage {_nameTranslator.MainStorage} {rightSymbol.SaveName}", false));
            }

            MinecraftFunction.Builder macro;
            var entity = _nameTranslator.MathEntity1.ToString();

            switch (kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Add, out bool isCreated);
                        if (isCreated)
                            macro.AddMacro($"execute positioned ~ $(a) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(b) {DEBUG_CHUNK_Z}");
                        break;
                    }
                case BoundBinaryOperatorKind.Subtraction:
                    {
                        var last = new EmittionVariableSymbol("*last", TypeSymbol.Object);
                        var pol = new EmittionVariableSymbol("*pol", TypeSymbol.Object);
                        functionBuilder.AddLocal(last);
                        functionBuilder.AddLocal(pol);

                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Subtract, out bool isCreated);
                        var sub = macro.SubFunctions?.FirstOrDefault(n => n.Name == $"{macro.Name}_if_minus");

                        if (sub == null)
                        {
                            sub = macro.CreateSubNamed("if_minus");
                            sub.AddCommand($"data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set string storage {_nameTranslator.MainStorage} {macroA.SaveName} 1");
                            sub.AddCommand($"data modify storage {_nameTranslator.MainStorage} {last.SaveName} set string storage {_nameTranslator.MainStorage} {macroA.SaveName} -1");
                            sub.AddCommand($"execute if data storage {_nameTranslator.MainStorage} {{ {last.SaveName} : \"d\" }} run data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set string storage {_nameTranslator.MainStorage} {macroA.SaveName} 0 -1");
                            sub.AddCommand($"execute if data storage {_nameTranslator.MainStorage} {{ {last.SaveName} : \"f\" }} run data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set string storage {_nameTranslator.MainStorage} {macroA.SaveName} 0 -1");
                            sub.AddCommand($"data modify storage {_nameTranslator.MainStorage} {macroPolarity.SaveName} set value \"\"");
                        }

                        if (isCreated)
                            macro.AddMacro($"execute positioned ~ $(b) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(polarity)$(a) {DEBUG_CHUNK_Z}");

                        blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroB.SaveName} set from storage {_nameTranslator.MainStorage} {symbol.SaveName}", false));
                        blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroA.SaveName} set from storage {_nameTranslator.MainStorage} {rightSymbol.SaveName}", false));
                        blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {pol.SaveName} set from storage {_nameTranslator.MainStorage} {rightSymbol.SaveName}", false));
                        blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {pol.SaveName} set string storage {_nameTranslator.MainStorage} {pol.SaveName} 0 1", false));
                        blockBuilder.Add(new TextCommand($"execute if data storage {_nameTranslator.MainStorage} {{ {pol.SaveName} : \"-\" }} run function {_nameTranslator.GetCallLink(sub)}", false));
                        blockBuilder.Add(new TextCommand($"execute unless data storage {_nameTranslator.MainStorage} {{ {pol.SaveName} : \"-\" }} run data modify storage {_nameTranslator.MainStorage} {macroPolarity.SaveName} set value \"-\"", false));

                        blockBuilder.Add(GetDoubleConversion(functionBuilder, macroA, macroA));
                        break;
                    }
                case BoundBinaryOperatorKind.Multiplication:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Multiply, out bool isCreated);

                        if (isCreated)
                        {
                            macro.AddMacro($"data modify storage {_nameTranslator.MainStorage} {_returnValue.SaveName} set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f]");
                            macro.AddMacro($"data modify entity {entity} transformation set value [0f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,$(b)f]");
                        }
                        break;
                    }
                case BoundBinaryOperatorKind.Division:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Divide, out bool isCreated);

                        if (isCreated)
                            macro.AddMacro($"data modify entity {entity} transformation set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,$(b)f]");
                        break;
                    }

                default:
                    throw new Exception($"Unexpected binary operation kind {kind}");
            }

            blockBuilder.Add(new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false));

            if (kind == BoundBinaryOperatorKind.Addition || kind == BoundBinaryOperatorKind.Subtraction)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set from entity {_nameTranslator.MathEntity1.ToString()} Pos[1]", false));
                blockBuilder.Add(new TextCommand($"tp {entity} {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z}", false));

                if (left.Type == TypeSymbol.Float)
                    blockBuilder.Add(GetFloatConversion(functionBuilder, symbol, symbol));

            }
            else if (kind == BoundBinaryOperatorKind.Multiplication)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {_returnValue.SaveName}[-1] set from entity {entity} transformation.translation[0]", false));
                blockBuilder.Add(new TextCommand($"data modify entity {entity} transformation set from storage {_nameTranslator.MainStorage} {_returnValue.SaveName}", false));
                blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set from entity {entity} transformation.translation[0]", false));
                functionBuilder.AddLocal(_returnValue);

                if (left.Type == TypeSymbol.Double)
                    blockBuilder.Add(GetDoubleConversion(functionBuilder, symbol, symbol));
            }
            else if (kind == BoundBinaryOperatorKind.Division)
            {
                blockBuilder.Add(new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set from entity {entity} transformation.translation[0]", false));

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

            if (call.Function.ReturnType == TypeSymbol.Int || call.Function.ReturnType == TypeSymbol.Bool || call.Function.ReturnType is EnumSymbol)
            {
                var emittion = TryGetBuiltInFunctionEmittion(functionBuilder, symbol, call, current);

                if (emittion != null)
                    return emittion;

                return Block(
                        Comment($"Assigning return value of {call.Function.Name} to \"{symbol.SaveName}\""),
                        GetFunctionParametersAssignment(functionBuilder, call.Function.Parameters, call.Arguments),
                        new TextCommand($"execute store result score {symbol.SaveName} {Vars} run function {_nameTranslator.GetCallLink(call.Function)}", false)
                    );
                //EmitFunctionParameterCleanUp(setParameters, emittion);    
            }
            else
            {
                return Block(
                    Comment($"Assigning return value of {call.Function.Name} to \"{symbol.SaveName}\""),
                    GetCallExpression(functionBuilder, symbol, call, current),
                    new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set from storage {_nameTranslator.MainStorage} {_returnValue.SaveName}", false)
                );
            }
        }

        private TextEmittionNode GetFunctionParametersAssignment(MinecraftFunction.Builder functionBuilder, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<BoundExpression> arguments)
        {
            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var parameter = parameters[i];

                var parameterVar = ToEmittionVariable(parameter);
                functionBuilder.AddLocal(parameterVar);
                var assignment = GetAssignment(functionBuilder, parameterVar, argument, 0);

                builder.Add(assignment);
            }

            if (builder.Count == 1)
                return builder.First();
            else
                return new TextBlockEmittionNode(builder.ToImmutable());
        }

        /*
        private void EmitFunctionParameterCleanUp(Dictionary<ParameterSymbol, string> parameters, MinecraftFunction emittion)
        {
            foreach (var parameter in parameters.Keys)
            {
                var name = parameters[parameter];
                EmitCleanUp(name, parameter.Type, emittion);
            }
        }
        */

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
                var temp = Temp(conversion.Expression.Type, current);
                var tempAssignment = GetAssignment(functionBuilder, temp, conversion.Expression, current);
                functionBuilder.AddLocal(temp);

                if (resultType == TypeSymbol.String)
                {
                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        var temp2 = Temp(TypeSymbol.String, current + 1);
                        functionBuilder.AddLocal(temp2);

                        return Block(
                                tempAssignment,
                                GetVariableAssignment(functionBuilder, temp2, temp),
                                new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set string storage {_nameTranslator.MainStorage} {temp2.SaveName}", false)
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
                                var command = new TextCommand($"execute if score {temp.SaveName} {Vars} matches {intMember.UnderlyingValue} run data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set value \"{enumMember.Name}\"", false);
                                builder.Add(command);
                            }
                            return new TextBlockEmittionNode(builder.ToImmutable());
                        }
                        else
                        {
                            return Block(
                                    tempAssignment,
                                    GetVariableAssignment(functionBuilder, symbol, temp)
                                );
                        }
                    }
                    else if (sourceType == TypeSymbol.Float || sourceType == TypeSymbol.Double)
                    {
                        return new TextCommand($"data modify storage {_nameTranslator.MainStorage} {symbol.SaveName} set string storage {_nameTranslator.MainStorage} {temp.SaveName}", false);
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
                            GetVariableAssignment(functionBuilder, symbol, temp)
                        );
                    }
                    else
                    {
                        return Block(
                            tempAssignment,
                            GetVariableAssignment(functionBuilder, symbol, temp)
                        );
                    }
                }
            }
        }
        private TextEmittionNode GetFloatConversion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToFloat, out bool isCreated);

            if (isCreated)
                macro.AddMacro($"data modify storage {_nameTranslator.MainStorage} {_returnValue.SaveName} set value $(a)f");

            var macroA = Macro("a");
            var copyCommand = GetVariableAssignment(functionBuilder, macroA, right);
            functionBuilder.AddLocal(macroA);

            return Block(
                    copyCommand,
                    new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false),
                    GetVariableAssignment(functionBuilder, left, _returnValue)
                );
        }

        private TextEmittionNode GetDoubleConversion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol left, EmittionVariableSymbol right)
        {
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToDouble, out bool isCreated);

            if (isCreated)
                macro.AddMacro($"data modify storage {_nameTranslator.MainStorage} {_returnValue.SaveName} set value $(a)d");

            var macroA = Macro("a");
            var copyCommand = GetVariableAssignment(functionBuilder, macroA, right);
            functionBuilder.AddLocal(_macro);

            return Block(
                    copyCommand,
                    new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false),
                    GetVariableAssignment(functionBuilder, left, _returnValue)
                );
        }

        private EmittionVariableSymbol Temp(TypeSymbol type, int current, string tempName = "temp", EmittionVariableLocation? location = null)
        {
            var name = $"*{tempName}{current}";

            if (!_variables.TryGetValue(name, out var emittionVariable))
            {
                emittionVariable = new EmittionVariableSymbol(name, type, location);
                _variables.Add(name, emittionVariable);
            }
            return emittionVariable;
        }

        private EmittionVariableSymbol Macro(string subName) => new EmittionVariableSymbol($"{_macro.Name}.{subName}", _macro.Type, _macro.Location);

        //private EmittionVariableSymbol EmitAssignmentToTemp(BoundExpression expression, MinecraftFunction emittion, int index) => EmitAssignmentToTemp(TEMP, expression, emittion, index);

        /*private EmittionVariableSymbol EmitAssignmentToTemp(string tempName, BoundExpression expression, MinecraftFunction emittion, int index)
        {
            var varName = $"{tempName}{index}";
            var tempSymbol = new EmittionVariableSymbol(varName, expression.Type);
            var resultName = EmitAssignmentExpression(varName, expression, emittion, index);
            return tempSymbol;
        }*/

        private MinecraftFunction.Builder GetOrCreateBuiltIn(FunctionSymbol function, out bool isCreated)
        {
            MinecraftFunction.Builder emittion;
            isCreated = !_usedBuiltIn.ContainsKey(function);

            if (isCreated)
            {
                emittion = new MinecraftFunction.Builder(function.Name, function, null);
                _usedBuiltIn.Add(function, emittion);
            }
            else
                emittion = _usedBuiltIn[function];

            return emittion;
        }

        public TextEmittionNode? TryEmitBuiltInFieldGetter(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundFieldAccessExpression right, int current)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(right.Field))
            {
                return new TextCommand($"execute store result score {symbol.SaveName} {Vars} run gamerule {right.Field.Name}", false);
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == right.Field)
            {
                return new TextCommand($"execute store result score {symbol.SaveName} {Vars} run difficulty", false);
            }
            return null;
        }

        public TextEmittionNode? TryEmitBuiltInFieldAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int current)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(field))
            {
                return EmitGameruleAssignment(functionBuilder, field, right, current);
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == field)
            {
                return EmitDifficultyAssignment(functionBuilder, field, right, current);
            }
            return null;
        }

        private TextEmittionNode EmitGameruleAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int tempIndex)
        {
            //1. Evaluate right to temp
            //2. If is bool, generate two conditions
            //3. If it's an int, generate a macro

            var temp = Temp(right.Type, tempIndex);
            var assignment = GetAssignment(functionBuilder, temp, right, tempIndex);

            if (field.Type == TypeSymbol.Bool)
            {
                return Block(
                        assignment,
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 1 run gamerule {field.Name} true", false),
                        new TextCommand($"execute if score {temp.SaveName} {Vars} matches 0 run gamerule {field.Name} false", false)
                    );
            }
            else
            {
                var macroRule = Macro("rule");
                var macroValue = Macro("value");

                var macroFunctionSymbol = BuiltInNamespace.Minecraft.General.Gamerules.SetGamerule;
                var macro = GetOrCreateBuiltIn(macroFunctionSymbol, out bool isCreated);

                if (isCreated)
                    macro.AddMacro("gamerule $(rule) $(value)");

                //EmitMacroCleanUp(emittion);

                return Block(
                        assignment,
                        new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroRule.SaveName} set value \"{field.Name}\"", false),
                        new TextCommand($"execute store result storage {_nameTranslator.MainStorage} {macroValue.SaveName} int 1 run scoreboard players get {temp.SaveName} {Vars}", false),
                        new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false)
                    );
            }
        }

        private TextEmittionNode EmitDifficultyAssignment(MinecraftFunction.Builder functionBuilder, FieldSymbol field, BoundExpression right, int tempIndex)
        {
            if (right is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
            {
                return new TextCommand($"difficulty {em.Name.ToLower()}", false);
            }

            var builder = ImmutableArray.CreateBuilder<TextEmittionNode>();
            var rightSymbol = Temp(right.Type, tempIndex);
            builder.Add(GetAssignment(functionBuilder, rightSymbol, right, tempIndex));

            foreach (var enumMember in BuiltInNamespace.Minecraft.General.Difficulty.Members)
            {
                var intMember = (IntEnumMemberSymbol)enumMember;
                builder.Add(new TextCommand($"execute if score {rightSymbol.SaveName} {Vars} matches {intMember.UnderlyingValue} run difficulty {enumMember.Name.ToLower()}", false));
            }

            //EmitCleanUp(rightName, right.Type, emittion);
            return new TextBlockEmittionNode(builder.ToImmutable());
        }

        public TextEmittionNode? TryGetBuiltInFunctionEmittion(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol? symbol, BoundCallExpression call, int tempIndex)
        {
            if (call.Function == BuiltInNamespace.Minecraft.General.RunCommand)
            {
                return EmitRunCommand(functionBuilder, call);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackEnable)
            {
                return EmitDatapackEnable(functionBuilder, call);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackDisable)
            {
                return EmitDatapackDisable(functionBuilder, call);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.SetDatapackEnabled)
            {
                return EmitSetDatapackEnabled(functionBuilder, call, tempIndex);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeather)
            {
                return EmitSetWeather(functionBuilder, call, tempIndex);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForTicks)
            {
                return EmitSetWeatherForTicks(functionBuilder, call, tempIndex);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForDays)
            {
                return EmitSetWeatherForDays(functionBuilder, call, tempIndex);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForSeconds)
            {
                return EmitSetWeatherForSeconds(functionBuilder, call, tempIndex);
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Say)
            {
                return EmitPrint(functionBuilder, call, tempIndex);
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Print)
            {
                return EmitPrint(functionBuilder, call, tempIndex);
            }

            if (symbol == null)
                return null;

            if (call.Function == BuiltInNamespace.Minecraft.General.GetDatapackCount)
            {
                return EmitGetDatapackCount(functionBuilder, symbol, call);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetEnabledDatapackCount)
            {
                return EmitGetDatapackCount(functionBuilder, symbol, call, true);
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetAvailableDatapackCount)
            {
                return EmitGetDatapackCount(functionBuilder, symbol, call, false, true);
            }
            return null;
        }

        private TextEmittionNode EmitRunCommand(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macroCommand = Macro("command");
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AddMacro("$(command)");

            return Block(
                    GetAssignment(functionBuilder, macroCommand, call.Arguments.First(), 0),
                    new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false)
                );
            // EmitMacroCleanUp();
        }

        private TextEmittionNode EmitDatapackEnable(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macrosPack = Macro("pack");
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AddMacro($"datapack enable \"file/$(pack)\"");

            return Block(
                    GetAssignment(functionBuilder, macrosPack, call.Arguments.First()),
                    new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false)
                );
            // EmitMacroCleanUp();
        }

        private TextEmittionNode EmitDatapackDisable(MinecraftFunction.Builder functionBuilder, BoundCallExpression call)
        {
            var macrosPack = Macro("pack");
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AddMacro($"datapack disable \"file/$(pack)\"");

            return Block(
                    GetAssignment(functionBuilder, macrosPack, call.Arguments.First()),
                    new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} {_macro.SaveName}", false)
                );
            // EmitMacroCleanUp();
        }

        private TextEmittionNode EmitSetDatapackEnabled(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex)
        {
            var pack = call.Arguments[0];
            var value = call.Arguments[1];

            var macrosPack = Macro("pack");
            var temp = Temp(TypeSymbol.Bool, tempIndex, "de");

            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
            {
                macro.AddMacro($"execute if score {temp.SaveName} {Vars} matches 1 run return run datapack enable \"file/$(pack)\"");
                macro.AddMacro($"datapack disable \"file/$(pack)\"");
            }

            return Block(
                    GetAssignment(functionBuilder, macrosPack, pack, tempIndex),
                    GetAssignment(functionBuilder, temp, value, tempIndex),
                    new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false)
                );

            // EmitCleanUp(valueName, value.Type);
            // EmitMacroCleanUp();
        }

        private TextEmittionNode EmitGetDatapackCount(MinecraftFunction.Builder functionBuilder, EmittionVariableSymbol symbol, BoundCallExpression call, bool countEnabled = false, bool countAvailable = false)
        {
            string filter = countEnabled ? "enabled" : "available";
            return new TextCommand($"execute store result score {symbol.SaveName} {Vars} run datapack list {filter}", false);
        }

        private TextEmittionNode EmitSetWeather(MinecraftFunction.Builder functionBuilder, BoundCallExpression call, int tempIndex, string? timeUnits = null)
        {
            TextEmittionNode EmitNonMacroNonConstantTypeCheck(BoundExpression weatherType, int current, int time = 0, string? timeUnits = null)
            {
                var temp = Temp(weatherType.Type, current, "type");
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
                // EmitCleanUp(right, weatherType.Type);
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
                    var macroType = Macro("type");
                    var duration = Temp(call.Arguments[1].Type, tempIndex);
                    var macroDuration = Macro("duration");
                    var macroTimeUnits = Macro("timeUnits");

                    var macro = GetOrCreateBuiltIn(BuiltInNamespace.Minecraft.General.Weather.SetWeather, out bool isCreated);
                    if (isCreated)
                        macro.AddMacro($"weather $(type) $(duration)(timeUnits)");

                    return Block(
                            GetAssignment(functionBuilder, macroType, weatherType, tempIndex),
                            GetAssignment(functionBuilder, duration, call.Arguments[1], tempIndex),
                            new TextCommand($"execute store result storage {_nameTranslator.MainStorage} {macroDuration.SaveName} int 1 run scoreboard players get {duration.SaveName} {Vars}", false),
                            new TextCommand($"data modify storage {_nameTranslator.MainStorage} {macroTimeUnits.SaveName} set value \"{timeUnits}\"", false),
                            new TextCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} {_macro.SaveName}", false)
                        );


                    // EmitCleanUp(duration, TypeSymbol.Int);
                    // EmitMacroCleanUp();
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
                var varSymbol = ToEmittionVariable(functionBuilder, variable, out var setupNode, tempIndex);
                return Block(
                        setupNode ?? TextTriviaNode.Empty(),
                        new TextCommand($"tellraw @a {{\"storage\":\"{_nameTranslator.MainStorage}\",\"nbt\":\"{varSymbol.SaveName}\"}}", false)
                    );
            }
            else
            {
                var temp = Temp(argument.Type, 0);

                return Block(
                        GetAssignment(functionBuilder, temp, argument, tempIndex),
                        new TextCommand($"tellraw @a {{\"storage\":\"{_nameTranslator.MainStorage}\",\"nbt\":\"{temp.SaveName}\"}}", false)
                    );
                // EmitCleanUp(tempName, argument.Type);
            }
        }
    }
}
