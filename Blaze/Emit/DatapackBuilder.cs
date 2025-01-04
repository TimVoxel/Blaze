using Blaze.Binding;
using Blaze.Emit.NameTranslation;
using Blaze.Emit.Nodes;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Blaze.Emit
{
    internal sealed class DatapackBuilder
    {
        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;
        private readonly EmittionNameTranslator _nameTranslator;

        private readonly MinecraftFunction _initFunction;
        private readonly MinecraftFunction _tickFunction;

        private readonly List<FunctionSymbol> _fabricatedMacroFunctions = new List<FunctionSymbol>();
        private readonly Dictionary<FunctionSymbol, MinecraftFunction> _usedBuiltIn = new Dictionary<FunctionSymbol, MinecraftFunction>();

        private string? _contextName = null;

        private string Vars => _nameTranslator.Vars;
        private string Const => _nameTranslator.Const;
        private string TEMP => EmittionNameTranslator.TEMP;
        private string RETURN_TEMP_NAME => EmittionNameTranslator.RETURN_TEMP_NAME;
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
        
        public Datapack BuildDatapack()
        {
            var functionNamespaceEmittionBuilder = ImmutableArray.CreateBuilder<NamespaceEmittionNode>();

            foreach (var ns in _program.Namespaces)
            {
                var namespaceSymbol = ns.Key;
                var boundNamespace = ns.Value;

                var namespaceEmittion = EmitNamespace(namespaceSymbol, boundNamespace);
                functionNamespaceEmittionBuilder.Add(namespaceEmittion);
            }

            foreach (var ns in _program.GlobalNamespace.NestedNamespaces)
            {
                if (ns.IsBuiltIn)
                {
                    var emittion = EmitBuiltInNamespace(ns);
                    if (emittion != null)
                        functionNamespaceEmittionBuilder.Add(emittion);
                }
            }

            var datapack = new Datapack(functionNamespaceEmittionBuilder.ToImmutable(), _configuration, _program.Diagnostics, _initFunction, _tickFunction);
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

        private NamespaceEmittionNode? EmitBuiltInNamespace(NamespaceSymbol ns, NamespaceEmittionNode? parent = null)
        {
            //We do this so that we do not generate unused functions and folders

            ImmutableArray<StructureEmittionNode>.Builder? childrenBuilder = null;

            foreach (var function in ns.Functions)
            {
                if (_usedBuiltIn.ContainsKey(function))
                {
                    if (childrenBuilder == null)
                        childrenBuilder = ImmutableArray.CreateBuilder<StructureEmittionNode>();

                    var emittion = _usedBuiltIn[function];
                    childrenBuilder.Add(emittion);
                }
            }

            foreach (var child in ns.NestedNamespaces)
            {
                var emittion = EmitBuiltInNamespace(child, parent);
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

        private NamespaceEmittionNode EmitNamespace(NamespaceSymbol symbol, BoundNamespace boundNamespace)
        {
            var childrenBuilder = ImmutableArray.CreateBuilder<StructureEmittionNode>();
            var ns = new BoundNamespaceExpression(symbol);
            
            foreach (var field in symbol.Fields)
            {
                if (field.Initializer == null)
                    continue;

                var fieldAccess = new BoundFieldAccessExpression(ns, field);
                var name = GetNameOfAssignableExpression(fieldAccess);
                EmitAssignmentExpression(name, field.Initializer, _initFunction, 0);
            }

            foreach (var function in boundNamespace.Functions)
            {
                var functionEmittion = EmitFunction(function.Key, function.Value);
                childrenBuilder.Add(functionEmittion);
            }

            foreach (var child in boundNamespace.Children)
            {
                var nestedNamespace = EmitNamespace(child.Key, child.Value);
                childrenBuilder.Add(nestedNamespace);
            }

            var namespaceEmittion = new NamespaceEmittionNode(symbol, symbol.Name, childrenBuilder.ToImmutable());
            return namespaceEmittion;
        }

        private MinecraftFunction EmitFunction(FunctionSymbol function, BoundStatement bodyBlock)
        {
            var emittion = new MinecraftFunction(function.Name, function, null);

            if (function.IsLoad)
                _initFunction.AddCommand($"function {_nameTranslator.GetCallLink(function)}");

            if (function.IsTick)
                _tickFunction.AddCommand($"function {_nameTranslator.GetCallLink(function)}");

            EmitStatement(bodyBlock, emittion);

            if (emittion.Content.FirstOrDefault(n => n.IsCleanUp) != null)
                emittion.Content.Add(new CleanUpMarkerEmittionNode());

            return emittion;
        }

        private void EmitStatement(BoundStatement node, MinecraftFunction emittion)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.NopStatement:
                    EmitNopStatement(emittion);
                    break;
                case BoundNodeKind.BlockStatement:
                    EmitBlockStatement((BoundBlockStatement)node, emittion);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EmitExpressionStatement((BoundExpressionStatement)node, emittion);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EmitVariableDeclarationStatement((BoundVariableDeclarationStatement)node, emittion);
                    break;
                case BoundNodeKind.IfStatement:
                    EmitIfStatement((BoundIfStatement)node, emittion);
                    break;
                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement)node, emittion);
                    break;
                case BoundNodeKind.DoWhileStatement:
                    EvaluateDoWhileStatement((BoundDoWhileStatement)node, emittion);
                    break;
                case BoundNodeKind.BreakStatement:
                    EmitBreakStatement((BoundBreakStatement)node, emittion);
                    break;
                case BoundNodeKind.ContinueStatement:
                    EmitContinueStatement((BoundContinueStatement)node, emittion);
                    break;
                case BoundNodeKind.ReturnStatement:
                    EmitReturnStatement((BoundReturnStatement)node, emittion);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private void EmitBlockStatement(BoundBlockStatement node, MinecraftFunction emittion)
        {
            foreach (BoundStatement statement in node.Statements)
                EmitStatement(statement, emittion);
        }

        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement node, MinecraftFunction emittion)
        {
            var name = _nameTranslator.GetVariableName(node.Variable);
            EmitAssignmentExpression(name, node.Initializer, emittion, 0);
        }

        private void EmitIfStatement(BoundIfStatement node, MinecraftFunction emittion)
        {
            //Emit condition into <.temp>
            //execute if <.temp> run subfunction
            //else generate a sub function and run it instead
            //if there is an else clause generate another sub with the else body

            var subFunction = emittion.CreateSub(SubFunctionKind.If);
            EmitStatement(node.Body, subFunction);

            var tempName = EmitAssignmentToTemp(node.Condition, emittion, 0);
            var callClauseCommand = $"execute if score {tempName} {Vars} matches 1 run function {_nameTranslator.GetCallLink(subFunction)}";

            emittion.AddCommand(callClauseCommand);

            if (node.ElseBody != null)
            {
                var elseSubFunction = emittion.CreateSub(SubFunctionKind.Else);
                EmitStatement(node.ElseBody, elseSubFunction);
                var elseCallClauseCommand = $"execute if score {tempName} {Vars} matches 0 run function {_nameTranslator.GetCallLink(elseSubFunction)}";

                emittion.AddCommand(elseCallClauseCommand);
            }
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
        }

        private void EvaluateWhileStatement(BoundWhileStatement node, MinecraftFunction emittion)
        {
            //Main:
            //Call sub function
            //
            //Generate body sub function:
            //Emit condition into <.temp>
            //execute if <.temp> run return 0
            //body

            var subFunction = emittion.CreateSub(SubFunctionKind.Loop);
            var callCommand = $"function {_nameTranslator.GetCallLink(subFunction)}";

            var tempName = EmitAssignmentToTemp(node.Condition, subFunction, 0);
            var breakClauseCommand = $"execute if score {tempName} {Vars} matches 0 run return 0";
            subFunction.AddCommand(breakClauseCommand);
            subFunction.AddLineBreak();

            EmitStatement(node.Body, subFunction);
            subFunction.AddLineBreak();
            subFunction.AddCommand(callCommand);
            emittion.AddCommand(callCommand);

            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
        }

        private void EvaluateDoWhileStatement(BoundDoWhileStatement node, MinecraftFunction emittion)
        {
            //Main:
            //Call sub function
            //
            //Generate body sub function:
            //body
            //Emit condition into <.temp>
            //execute if <.temp> run function <subfunction>

            var subFunction = emittion.CreateSub(SubFunctionKind.Loop);
            var callCommand = $"function {_nameTranslator.GetCallLink(subFunction)}";

            EmitStatement(node.Body, subFunction);

            var tempName = EmitAssignmentToTemp(node.Condition, subFunction, 0);
            var loopClauseCommand = $"execute if score {tempName} {Vars} matches 1 run {callCommand}";
            subFunction.AddCommand(loopClauseCommand);
            subFunction.AddLineBreak();

            emittion.AddCommand(callCommand);
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
        }

        private void EmitContinueStatement(BoundContinueStatement node, MinecraftFunction emittion)
        {
            throw new NotImplementedException();
        }

        private void EmitBreakStatement(BoundBreakStatement node, MinecraftFunction emittion)
        {
            throw new NotImplementedException();
        }

        private void EmitReturnStatement(BoundReturnStatement node, MinecraftFunction emittion)
        {
            //Emit cleanup before we break the function
            //Assign the return value to <return.value>
            //If the return value is an integer or a bool, return it

            void EmitCleanUp()
            {
                emittion.AddComment("Clean up before break");
                emittion.Content.Add(new CleanUpMarkerEmittionNode());
                emittion.AddLineBreak();
            }

            var returnExpression = node.Expression;
            if (returnExpression == null)
            {
                EmitCleanUp();
                emittion.AddCommand("return 0");
                return;
            }

            var desiredReturnName = (returnExpression.Type is NamedTypeSymbol && _contextName != null) ? _contextName : EmittionNameTranslator.RETURN_TEMP_NAME;
            var returnName = EmitAssignmentExpression(desiredReturnName, returnExpression, emittion, 0);
            EmitCleanUp();

            if (returnExpression.Type == TypeSymbol.Int || returnExpression.Type == TypeSymbol.Bool || returnExpression.Type is EnumSymbol e && e.IsIntEnum)
            {
                var returnCommand = $"return run scoreboard players get {returnName} {Vars}";
                emittion.AddCommand(returnCommand);
            }
            else
            {
                emittion.AddCommand("return 0");
            }
        }

        private void EmitExpressionStatement(BoundExpressionStatement node, MinecraftFunction emittion)
        {
            //Can be either call or assignment
            var expression = node.Expression;

            if (expression is BoundAssignmentExpression assignment)
            {
                EmitAssignmentExpression(assignment, emittion, 0);
            }
            else if (expression is BoundCallExpression call)
            {
                EmitCallExpression(null, call, emittion, 0);
            }
            else
            {
                throw new Exception($"Unexpected expression statement kind {expression.Kind}");
            }
        }

        private void EmitCallExpression(string? name, BoundCallExpression call, MinecraftFunction emittion, int current)
        {
            //Can be a built-in function -> TryEmitBuiltInFunction();
            //Can be a user defined function ->
            //
            //Assign every parameter to temp variable
            //function <function>
            //Reset every parameter

            var isBuiltIt = TryEmitBuiltInFunction(name, call, emittion, current);
            if (!isBuiltIt)
            {
                var setNames = EmitFunctionParametersAssignment(call.Function.Parameters, call.Arguments, emittion);
                var command = $"function {_nameTranslator.GetCallLink(call.Function)}";
                emittion.AddCommand(command);

                EmitFunctionParameterCleanUp(setNames, emittion);
            }
        }

        private void EmitNopStatement(MinecraftFunction emittion)
        {
            emittion.AddCommand("tellraw @a {\"text\":\"Nop statement in program\", \"color\":\"red\"}");
        }

        private string EmitAssignmentExpression(BoundAssignmentExpression assignment, MinecraftFunction emittion, int current)
            => EmitAssignmentExpression(assignment.Left, assignment.Right, emittion, current);

        private string EmitAssignmentExpression(BoundExpression left, BoundExpression right, MinecraftFunction emittion, int current)
        {
            //Built-in fields may require non-scoreboard approachers
            //Such fields include gamerules, difficulty and other things
            //Entity and block data management requires the /data command

            //TODO: Add Reclassifator and actual corresponding nodes that can
            //Be translated pretty much one to one

            if (left is BoundFieldAccessExpression fieldAccess)
            {
                string? tempName;
                if (TryEmitBuiltInFieldAssignment(fieldAccess.Field, right, emittion, current, out tempName))
                {
                    //This is a problem that needs to be addressed
                    if (tempName == null)
                        return string.Empty;
                    return tempName;
                }
            }

            var leftName = GetNameOfAssignableExpression(left, emittion, current);
            return EmitAssignmentExpression(leftName, right, emittion, current);
        }

        private string EmitAssignmentExpression(VariableSymbol variable, BoundExpression right, MinecraftFunction emittion, int current)
            => EmitAssignmentExpression(_nameTranslator.GetVariableName(variable), right, emittion, current);

        private string EmitAssignmentExpression(string name, BoundExpression right, MinecraftFunction emittion, int current)
        {
            if (right is BoundLiteralExpression l)
            {
                EmitLiteralAssignment(name, l, emittion);
            }
            else if (right is BoundVariableExpression v)
            {
                EmitVariableAssignment(name, v.Variable, emittion);
            }
            else if (right is BoundAssignmentExpression a)
            {
                var otherName = EmitAssignmentExpression(a, emittion, current);
                EmitVariableAssignment(name, otherName, a.Type, emittion);
            }
            else if (right is BoundUnaryExpression u)
            {
                EmitUnaryExpressionAssignment(name, u, emittion, current);
            }
            else if (right is BoundBinaryExpression b)
            {
                EmitBinaryExpressionAssignment(name, b, emittion, current);
            }
            else if (right is BoundCallExpression c)
            {
                EmitCallExpressionAssignment(name, c, emittion, current);
            }
            else if (right is BoundConversionExpression conv)
            {
                EmitConversionExpressionAssignment(name, conv, emittion, current);
            }
            else if (right is BoundObjectCreationExpression objectCreation)
            {
                EmitObjectCreationAssignment(name, objectCreation, emittion, current);
            }
            else if (right is BoundArrayCreationExpression arrayCreation)
            {
                EmitArrayCreationAssignment(name, arrayCreation, emittion, current);
            }
            else if (right is BoundFieldAccessExpression fieldExpression)
            {
                if (!TryEmitBuiltInFieldGetter(name, fieldExpression, emittion, current))
                {
                    var otherName = GetNameOfAssignableExpression(fieldExpression, emittion, current);
                    EmitVariableAssignment(name, otherName, right.Type, emittion);
                }
            }
            else if (right is BoundArrayAccessExpression arrayAccessExpression)
            {
                EmitArrayAccessAssignment(name, arrayAccessExpression, emittion, current);
            }
            else
            {
                throw new Exception($"Unexpected expression kind {right.Kind}");
            }
            return name;
        }

        private string GetNameOfAssignableExpression(BoundExpression left, MinecraftFunction? emittion = null, int? tempIndex = null)
        {
            if (left is BoundVariableExpression v)
            {
                return _nameTranslator.GetVariableName(v.Variable);
            }

            var leftAssociativeOrder = new Stack<BoundExpression>();
            leftAssociativeOrder.Push(left);

            /*
            if (left is BoundFieldAccessExpression fa)
                leftAssociativeOrder.Push(fa);
            else if (left is BoundArrayAccessExpression aa)
                leftAssociativeOrder.Push(aa);
            else
                throw new Exception($"Unexpected bound expression kind {left.Kind}");
            */

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
            //Scoreboards allow us to do that, and storages have built-in nesting functionality

            var nameBuilder = new StringBuilder();

            while (leftAssociativeOrder.Any())
            {
                var current = leftAssociativeOrder.Pop();

                //Some of the stuff we added might be side products
                //like BoundMethodAccessExpression, that can be just skipped

                if (current is BoundVariableExpression variableExpression)
                {
                    if (nameBuilder.Length == 0)
                        nameBuilder.Append(_nameTranslator.GetVariableName(variableExpression.Variable));
                    else
                        nameBuilder.Append(variableExpression.Variable.Name);
                }
                else if (current is BoundThisExpression thisExpression)
                {
                    nameBuilder.Append(_contextName);
                }
                else if (current is BoundNamespaceExpression namespaceExpression)
                {
                    nameBuilder.Append(_nameTranslator.GetNamespaceFieldPath(namespaceExpression.Namespace));
                }
                else if (current is BoundFieldAccessExpression fieldAccess)
                {
                    nameBuilder.Append($".{fieldAccess.Field.Name}");
                }
                else if (current is BoundArrayAccessExpression arrayAccess)
                {
                    if (tempIndex == null || emittion == null)
                        throw new Exception("Array access outside of a function");

                    //TODO: Allow non-constant array access in a more civilised implementation
                    foreach (var argument in arrayAccess.Arguments)
                    {
                        if (argument.ConstantValue == null)
                            throw new Exception("Non-consant array access  is not supported with the current implementation");

                        nameBuilder.Append($"[{argument.ConstantValue.Value}]");
                    }
                }

                //FunctionExpression -> do nothing
                //MethodAccessExpression -> do nothing
            }
            return nameBuilder.ToString();
        }

        private void EmitLiteralAssignment(string varName, BoundLiteralExpression literal, MinecraftFunction emittion)
        {
            //int literal       -> scoreboard players set *v integers <value>
            //string literal    -> data modify storage strings <name> set value "<value>"
            //bool literal      -> scoreboard players set *v bools <value>
            //float litreal     -> data modify storage doubles <name> set value <value>f
            //double litreal    -> data modify storage doubles <name> set value <value>d

            if (literal.Type == TypeSymbol.String)
            {
                var value = (string)literal.Value;
                value = value.Replace("\"", "\\\"");
                var command = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{varName}\" set value \"{value}\"";
                emittion.AddCommand(command);
            }
            else if (literal.Type == TypeSymbol.Float)
            {
                var value = (float)literal.Value;
                var command = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Float)} \"{varName}\" set value {value}f";
                emittion.AddCommand(command);
            }
            else if (literal.Type == TypeSymbol.Double)
            {
                var value = (double)literal.Value;
                var command = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Double)} \"{varName}\" set value {value}d";
                emittion.AddCommand(command);
            }
            else
            {
                int value;

                if (literal.Type == TypeSymbol.Int)
                    value = (int)literal.Value;
                else
                    value = ((bool)literal.Value) ? 1 : 0;

                var command = $"scoreboard players set {varName} {Vars} {value}";
                emittion.AddCommand(command);
            }
        }

        private void EmitVariableAssignment(string varName, VariableSymbol otherVar, MinecraftFunction emittion)
        {
            if (otherVar is StringEnumMemberSymbol stringEnumMember)
            {
                var storage = _nameTranslator.GetStorage(otherVar.Type);
                var value = stringEnumMember.UnderlyingValue;
                var command = $"data modify storage {storage} \"{varName}\" set value \"{value}\"";
                emittion.AddCommand(command);
            }
            else if (otherVar is IntEnumMemberSymbol intEnumMember)
            {
                var value = intEnumMember.UnderlyingValue;
                var command = $"scoreboard players set {varName} {Vars} {value}";
                emittion.AddCommand(command);
            }
            else
                EmitVariableAssignment(varName, _nameTranslator.GetVariableName(otherVar), otherVar.Type, emittion);
        }

        private void EmitArrayAccessAssignment(string name, BoundArrayAccessExpression arrayAccessExpression, MinecraftFunction emittion, int current)
        {
            //if all the arguments have constant values, just add [a][b][c]... to the accessed name
            //Otherwise, create a macro (or get an existing one) for the corresponding rank of the array
            //Set its arguments (and the name to access) to the corresponding values and copy the return value to the desired location

            emittion.AddComment($"Emitting array access to {name}, stored in variable \"*array\"\r\n");

            var rank = arrayAccessExpression.Arguments.Length;
            var macroName = $"array_access_rank{rank}";

            var accessedName = GetNameOfAssignableExpression(arrayAccessExpression.Identifier, emittion, current);

            var fabricatedAccessor = _fabricatedMacroFunctions.FirstOrDefault(f => f.Name == macroName);

            if (fabricatedAccessor == null)
            {
                fabricatedAccessor = new FunctionSymbol(macroName, BuiltInNamespace.Blaze.Fabricated.Symbol, ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Object, false, false, AccessModifier.Private, null);
                BuiltInNamespace.Blaze.Fabricated.Symbol.Members.Add(fabricatedAccessor);
                _fabricatedMacroFunctions.Add(fabricatedAccessor);
            }

            var accessorEmittion = GetOrCreateBuiltIn(fabricatedAccessor, out bool isCreated);
            var macroStorage = _nameTranslator.GetStorage(TypeSymbol.String);

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

                accessorEmittion.AddMacro($"data modify storage {macroStorage} \"{RETURN_TEMP_NAME}\" set from storage {_nameTranslator.MainStorage} $(name){accessRankBuilder.ToString()}");
            }

            emittion.AddCommand($"data modify storage {macroStorage} **macros.name set value \"{accessedName}\"");

            for (int i = 0; i < rank; i++)
            {
                var argumentName = $"**macros.a{i.ToString()}";
                var tempName = EmitAssignmentToTemp(arrayAccessExpression.Arguments[i], emittion, current + i);
                emittion.AddCommand($"execute store result storage {macroStorage} **macros.{"a" + i.ToString()} int 1 run scoreboard players get {tempName} {Vars}");
                EmitCleanUp(tempName, TypeSymbol.Int, emittion);
            }

            emittion.AddCommand($"function {_nameTranslator.GetCallLink(accessorEmittion)} with storage {macroStorage} **macros");
            EmitVariableAssignment(name, RETURN_TEMP_NAME, TypeSymbol.String, emittion);
            EmitMacroCleanUp(emittion);

            emittion.AddLineBreak();
        }

        private void EmitVariableAssignment(string varName, string otherName, TypeSymbol type, MinecraftFunction emittion)
        {
            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string, float, double literal    -> data modify storage strings *this set from storage strings *other
            //named type        -> assign all the fields to the corresponding ones of the object we are copying

            if (varName == otherName)
                return;

            if (IsStorageType(type) || type is ArrayTypeSymbol)
            {
                var storage = _nameTranslator.GetStorage(type);
                var command = $"data modify storage {storage} \"{varName}\" set from storage {storage} \"{otherName}\"";
                emittion.AddCommand(command);
            }
            else if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol intE)
            {
                var command = $"scoreboard players operation {varName} {Vars} = {otherName} {Vars}";
                emittion.AddCommand(command);
            }
            else
            {
                var namedType = (NamedTypeSymbol)type;
                foreach (var field in namedType.Fields)
                {
                    var targetFieldName = $"{varName}.{field.Name}";
                    var sourceFieldName = $"{otherName}.{field.Name}";
                    EmitVariableAssignment(targetFieldName, sourceFieldName, field.Type, emittion);
                }
            }
        }

        private void EmitObjectCreationAssignment(string varName, BoundObjectCreationExpression objectCreationExpression, MinecraftFunction emittion, int current)
        {
            //Reserve a name for an object
            //Execute the constructor with the arguments

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

            emittion.AddComment($"Emitting object creation of type {objectCreationExpression.NamedType.Name}, stored in reference variable {varName}");

            var constructor = objectCreationExpression.NamedType.Constructor;
            Debug.Assert(constructor != null);
            var setParameters = EmitFunctionParametersAssignment(constructor.Parameters, objectCreationExpression.Arguments, emittion);
            Debug.Assert(constructor.FunctionBody != null);

            //We do this so that the constructor block knows the "this" instance name
            var currentContextName = _contextName;
            _contextName = varName;
            EmitBlockStatement(constructor.FunctionBody, emittion);
            _contextName = currentContextName;

            EmitFunctionParameterCleanUp(setParameters, emittion);
        }

        private void EmitArrayCreationAssignment(string name, BoundArrayCreationExpression arrayCreationExpression, MinecraftFunction emittion, int current)
        {
            //in a for loop, append a default value to a tempI list
            //in a for loop, append result of that to a temp[i-1] list
            //if i == 0, don't use a temp list: use the desired name
            //Remove all the temp lists

            emittion.AddComment($"Emitting array creation of type {arrayCreationExpression.ArrayType}, stored in variable \"{name}\"");

            var defaultValue = GetEmittionDefaultValue(arrayCreationExpression.ArrayType.Type);
            var storage = _nameTranslator.GetStorage(arrayCreationExpression.ArrayType.Type);
            var previous = string.Empty;
            var usedTemps = new HashSet<string>();

            for (int i = arrayCreationExpression.Dimensions.Length - 1; i >= 0; i--)
            {
                string arrayName;
                if (i == 0)
                    arrayName = name;
                else
                {
                    arrayName = $"**rank{current + i}";
                    usedTemps.Add(arrayName);
                }

                string assignmentCommand;

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
                        assignmentCommand = $"data modify storage {storage} \"{arrayName}\" set value {initializerBuilder.ToString()}";

                        emittion.AddCommand(assignmentCommand);
                        previous = arrayName;
                        continue;
                    }
                    else
                        assignmentCommand = $"data modify storage {storage} \"{arrayName}\" append value {defaultValue}";
                }
                else
                    assignmentCommand = $"data modify storage {storage} \"{arrayName}\" append from storage {storage} {previous}";

                emittion.AddCommand($"data modify storage {storage} \"{arrayName}\" set value []");

                var subFunction = emittion.CreateSub(SubFunctionKind.Loop);
                var callCommand = $"function {_nameTranslator.GetCallLink(subFunction)}";

                var iterator = EmitAssignmentToTemp(".iter", new BoundLiteralExpression(0), emittion, current + i);
                var upperBound = EmitAssignmentToTemp(".upperBound", arrayCreationExpression.Dimensions[i], emittion, current + i);
                emittion.AddCommand(callCommand);

                if (previous != string.Empty)
                    emittion.AddCommand($"data remove storage {storage} \"{previous}\"");

                subFunction.AddCommand(assignmentCommand);
                subFunction.AddCommand($"scoreboard players add {iterator} {Vars} 1");
                subFunction.AddCommand($"execute if score {iterator} {Vars} < {upperBound} {Vars} run {callCommand}");

                previous = arrayName;
                EmitCleanUp(iterator, TypeSymbol.Int, emittion);
                EmitCleanUp(upperBound, TypeSymbol.Int, emittion);
            }
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

        private void EmitUnaryExpressionAssignment(string name, BoundUnaryExpression unary, MinecraftFunction emittion, int current)
        {
            //TODO: Add constant assignment to load function

            //Identity -> Assign the expression normally
            //Negation -> Assign the expression normally, than multiply it by -1
            //Logical negation
            //         -> Assign the expression to <.temp> variable
            //            If it is 1, set the <*name> to 0
            //            If it is 0, set the <*name> to 1

            emittion.AddLineBreak();
            emittion.AddComment($"Emitting unary expression \"{unary}\" to \"{name}\"");
            var operand = unary.Operand;
            var operatorKind = unary.Operator.OperatorKind;

            switch (operatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    EmitAssignmentExpression(name, operand, emittion, current);
                    break;
                case BoundUnaryOperatorKind.Negation:

                    var varName = EmitAssignmentExpression(name, operand, emittion, current);
                    if (operand.Type == TypeSymbol.Int)
                    {
                        var command = $"scoreboard players operation {varName} {Vars} *= *-1 {Const}";
                        emittion.AddCommand(command);
                    }
                    else
                    {
                        var storage = _nameTranslator.GetStorage(operand.Type);
                        var stringStorage = _nameTranslator.GetStorage(TypeSymbol.String);

                        emittion.AddCommand($"data modify storage {stringStorage} **macros.a set from storage {storage} {varName}");
                        emittion.AddCommand($"data modify storage {stringStorage} \"**sign\" set string storage {storage} {varName} 0 1");
                        emittion.AddCommand($"data modify storage {stringStorage} \"**last\" set string storage {storage} {varName} -1");
                        emittion.AddCommand($"execute if data storage {stringStorage} {{ \"**sign\": \"-\"}} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 1");

                        var typeSuffix = operand.Type == TypeSymbol.Float ? "f" : "d";
                        emittion.AddCommand($"execute if data storage {stringStorage} {{ \"**last\": \"{typeSuffix}\"}} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 0 -1");

                        emittion.AddCommand($"execute if data storage {stringStorage} {{ \"**sign\": \"-\"}} run data modify storage {stringStorage} **macros.sign set value \"\"");
                        emittion.AddCommand($"execute unless data storage {stringStorage} {{ \"**sign\": \"-\"}} run data modify storage {stringStorage} **macros.sign set value \"-\"");

                        MinecraftFunction macro;

                        if (operand.Type == TypeSymbol.Float)
                        {
                            macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateFloat, out bool isCreated);
                            if (isCreated)
                                macro.AddMacro($"data modify storage {stringStorage} **macros.return set value $(sign)$(a)f");
                        }
                        else
                        {
                            macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateDouble, out bool isCreated);
                            if (isCreated)
                                macro.AddMacro($"data modify storage {stringStorage} **macros.return set value $(sign)$(a)d");
                        }

                        emittion.AddCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} **macros");
                        emittion.AddCommand($"data modify storage {storage} {name} set from storage {stringStorage} **macros.return");

                        EmitCleanUp("**sign", TypeSymbol.String, emittion);
                        EmitCleanUp("**last", TypeSymbol.String, emittion);
                        EmitMacroCleanUp(emittion);
                    }
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:

                    var tempName = EmitAssignmentToTemp(operand, emittion, current);
                    var command1 = $"execute if score {tempName} {Vars} matches 1 run scoreboard players set {name} {Vars} 0";
                    var command2 = $"execute if score {tempName} {Vars} matches 0 run scoreboard players set {name} {Vars} 1";
                    emittion.AddCommand(command1);
                    emittion.AddCommand(command2);
                    EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
                    break;
                default:
                    throw new Exception($"Unexpected unary operator kind {operatorKind}");
            }
        }

        private void EmitBinaryExpressionAssignment(string name, BoundBinaryExpression binary, MinecraftFunction emittion, int current)
        {
            //TODO: Add constant assignment to load function

            //Addition ->  INT: Assign the left to <*name>, assign the right to <.right>, add the two via spo
            //             STRING: No clue, should research the subject

            //Subtraction ->    Assign the left to <*name>, assign the right to <.right>, subtract the two via spo
            //Multiplication -> Assign the left to <*name>, assign the right to <.right>, multiply the two via spo
            //Division ->       Assign the left to <*name>, assign the right to <.right>, divide the two via spo
            //LogicalAddition       -> Assign the left to <.left>, assign the right to <.right>
            //                         Set <*name> to 0
            //                         If left is 1 set <*name> to 1
            //                         If right is 1 set <*name> to 1

            //LogicalMultiplication -> Assign the left to <.left>, assign the right to <.right>
            //                         Set <*name> to 0
            //                         If left is 1 and right is 1 set <.name> to 1
            //Equals    -> INT: do the same as less and greater
            //          STRING: copy the value to some storage <temp> value
            //                  execute store success, invert the result
            //NotEquals -> INT: do the same as equals but with unless
            //          STRING: same as equals but do not invert the result
            //Less, LessOrEquals    -> Assign the left to <.left>, assign the right to <.right>
            //Greater, GreaterOrEquals Set < *name > to 0
            //                         if score < *left > is <sign> than <.right >, set < *name > to 1

            emittion.AddLineBreak();
            emittion.AddComment($"Emitting binary expression \"{binary}\" to \"{name}\"");

            var left = binary.Left;
            var right = binary.Right;
            var operatorKind = binary.Operator.OperatorKind;

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Addition:

                    if (left.Type == TypeSymbol.Int)
                    {
                        EmitIntBinaryOperation(name, emittion, left, right, operatorKind, current);
                    }
                    else if (left.Type == TypeSymbol.Float || left.Type == TypeSymbol.Double)
                    {
                        EmitFloatingPointBinaryOperation(name, emittion, left, right, BoundBinaryOperatorKind.Addition, current);
                    }
                    else
                    {
                        emittion.AddComment("String concatination is currently unsupported");
                    }
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                case BoundBinaryOperatorKind.Multiplication:
                case BoundBinaryOperatorKind.Division:
                    if (left.Type == TypeSymbol.Int)
                    {
                        EmitIntBinaryOperation(name, emittion, left, right, operatorKind, current);
                    }
                    else
                    {
                        EmitFloatingPointBinaryOperation(name, emittion, left, right, operatorKind, current);
                    }
                    break;
                case BoundBinaryOperatorKind.LogicalMultiplication:
                    {
                        var leftName = EmitAssignmentToTemp($"lbTemp", left, emittion, current + 1);
                        var rightName = EmitAssignmentToTemp($"rbTemp", right, emittion, current + 1);

                        var command1 = $"scoreboard players set {name} {Vars} 0";
                        var command2 = $"execute if score {leftName} {Vars} matches 1 if score {rightName} {Vars} matches 1 run scoreboard players set {name} {Vars} 1";
                        emittion.AddCommand(command1);
                        emittion.AddCommand(command2);
                        EmitCleanUp(leftName, left.Type, emittion);
                        EmitCleanUp(rightName, right.Type, emittion);
                    }
                    break;
                case BoundBinaryOperatorKind.LogicalAddition:
                    {
                        var leftName = EmitAssignmentToTemp($"lbTemp", left, emittion, current + 1);
                        var rightName = EmitAssignmentToTemp($"rbTemp", right, emittion, current + 1);

                        var command1 = $"scoreboard players set {name} {Vars} 0";
                        var command2 = $"execute if score {leftName} {Vars} matches 1 run scoreboard players set {name} {Vars} 1";
                        var command3 = $"execute if score {rightName} {Vars} matches 1 run scoreboard players set {name} {Vars} 1";
                        emittion.AddCommand(command1);
                        emittion.AddCommand(command2);
                        emittion.AddCommand(command3);
                        EmitCleanUp(leftName, left.Type, emittion);
                        EmitCleanUp(rightName, right.Type, emittion);
                    }
                    break;
                case BoundBinaryOperatorKind.Equals:
                    {
                        if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object || left.Type is EnumSymbol e && !e.IsIntEnum)
                        {
                            var leftName = EmitAssignmentToTemp("lTemp", left, emittion, current + 1, false);
                            var rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1, false);

                            var command1 = $"execute store success score {TEMP} {Vars} run data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{leftName}\" set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{rightName}\"";
                            var command2 = $"execute if score {TEMP} {Vars} matches 1 run scoreboard players set {name} {Vars} 0";
                            var command3 = $"execute if score {TEMP} {Vars} matches 0 run scoreboard players set {name} {Vars} 1";
                            emittion.AddCommand(command1);
                            emittion.AddCommand(command2);
                            emittion.AddCommand(command3);
                            EmitCleanUp(leftName, left.Type, emittion);
                            EmitCleanUp(rightName, right.Type, emittion);
                            EmitCleanUp(TEMP, TypeSymbol.Bool, emittion);
                        }
                        else if (left.Type == TypeSymbol.Float || left.Type == TypeSymbol.Double)
                        {
                            EmitFloatingPointComparisonOperation(emittion, name, left, right, operatorKind, current);
                        }
                        else
                        {
                            EmitIntComparisonOperation(emittion, left, right, name, operatorKind, current);
                        }
                    }
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    {
                        if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object || left.Type is EnumSymbol e && !e.IsIntEnum)
                        {
                            var leftName = EmitAssignmentToTemp("lTemp", left, emittion, current + 1, false);
                            var rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1, false);

                            var command1 = $"execute store success score {name} {Vars} run data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{leftName}\" set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{rightName}\"";
                            emittion.AddCommand(command1);
                            EmitCleanUp(leftName, left.Type, emittion);
                            EmitCleanUp(rightName, right.Type, emittion);
                        }
                        else
                        {
                            EmitIntComparisonOperation(emittion, left, right, name, operatorKind, current);
                        }
                    }
                    break;
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.LessOrEquals:
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    if (left.Type == TypeSymbol.Int)
                    {
                        EmitIntComparisonOperation(emittion, left, right, name, operatorKind, current);
                    }
                    else
                    {
                        EmitFloatingPointComparisonOperation(emittion, name, left, right, operatorKind, current);
                    }
                    break;
            }
        }

        private void EmitIntComparisonOperation(MinecraftFunction emittion, BoundExpression left, BoundExpression right, string name, BoundBinaryOperatorKind operation, int index)
        {
            var leftName = string.Empty;
            var initialValue = operation == BoundBinaryOperatorKind.NotEquals ? 1 : 0;
            var successValue = operation == BoundBinaryOperatorKind.NotEquals ? 0 : 1;

            if (left is BoundVariableExpression v)
            {
                //TODO: remove this when constant folding will be in place
                //This can only occur when two constants are compared
                if (v.Variable is IntEnumMemberSymbol enumMember)
                {
                    var other = (IntEnumMemberSymbol)((BoundVariableExpression)right).Variable;
                    var result = (other.UnderlyingValue == enumMember.UnderlyingValue) ? successValue : initialValue;
                    emittion.AddCommand($"scoreboard players set {name} {Vars} {result}");
                    return;
                }

                leftName = _nameTranslator.GetVariableName(v.Variable);
            }
            else
            {
                leftName = EmitAssignmentToTemp("lTemp", left, emittion, index + 1);
                EmitCleanUp(leftName, left.Type, emittion);
            }

            var command1 = $"scoreboard players set {name} {Vars} {initialValue}";
            var command2 = string.Empty;
            if (right is BoundLiteralExpression l && l.Value is int)
            {
                int value = (int)l.Value;
                var comparason = "matches " + operation switch
                {
                    BoundBinaryOperatorKind.Less => ".." + (value - 1).ToString(),
                    BoundBinaryOperatorKind.LessOrEquals => ".." + value,
                    BoundBinaryOperatorKind.Greater => (value + 1).ToString() + "..",
                    BoundBinaryOperatorKind.GreaterOrEquals => value + "..",
                    _ => value
                };
                command2 = $"execute unless score {leftName} {Vars} {comparason} run scoreboard players set {name} {Vars} {successValue}";
            }
            else
            {
                var rightName = string.Empty;
                if (right is BoundVariableExpression vr)
                {
                    if (vr.Variable is IntEnumMemberSymbol enumMember)
                    {
                        var memberUnderlyingValue = enumMember.UnderlyingValue;
                        command2 = $"execute if score {leftName} {Vars} matches {memberUnderlyingValue} run scoreboard players set {name} {Vars} {successValue}";
                        emittion.AddCommand(command1);
                        emittion.AddCommand(command2);
                        return;
                    }
                    rightName = _nameTranslator.GetVariableName(vr.Variable);
                }
                else
                {
                    rightName = EmitAssignmentToTemp("rTemp", right, emittion, index + 1);
                    EmitCleanUp(rightName, right.Type, emittion);
                }
                var operationSign = operation switch
                {
                    BoundBinaryOperatorKind.Less => "<",
                    BoundBinaryOperatorKind.LessOrEquals => "<=",
                    BoundBinaryOperatorKind.Greater => ">",
                    BoundBinaryOperatorKind.GreaterOrEquals => ">=",
                    _ => "="
                };
                command2 = $"execute if score {leftName} {Vars} {operationSign} {rightName} {Vars} run scoreboard players set {name} {Vars} {successValue}";
            }

            emittion.AddCommand(command1);
            emittion.AddCommand(command2);
        }

        private void EmitFloatingPointComparisonOperation(MinecraftFunction emittion, string name, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int current)
        {
            var stringStorage = _nameTranslator.GetStorage(TypeSymbol.String);
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.PositionY, out bool isCreated);
            if (isCreated)
                macro.AddMacro($"tp @s {DEBUG_CHUNK_X} $(a) {DEBUG_CHUNK_Z}");

            var temp = EmitAssignmentToTemp(TEMP, left, emittion, current + 1);
            emittion.AddCommand($"data modify storage {stringStorage} **macros.a set from storage {_nameTranslator.GetStorage(left.Type)} \"{temp}\"");
            emittion.AddCommand($"execute as {_nameTranslator.MathEntity1} run function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} **macros");

            temp = EmitAssignmentToTemp(TEMP, right, emittion, current + 1);
            emittion.AddCommand($"data modify storage {stringStorage} **macros.a set from storage {_nameTranslator.GetStorage(right.Type)} \"{temp}\"");
            emittion.AddCommand($"execute as {_nameTranslator.MathEntity2} run function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} **macros");

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Equals:
                    {
                        emittion.AddCommand($"scoreboard players set {name} {Vars} 0");
                        emittion.AddCommand($"execute as {_nameTranslator.MathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {name} {Vars} 1");
                        break;
                    }
                case BoundBinaryOperatorKind.NotEquals:
                    {
                        emittion.AddCommand($"scoreboard players set {name} {Vars} 1");
                        emittion.AddCommand($"execute as {_nameTranslator.MathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {name} {Vars} 0");
                        break;
                    }
                case BoundBinaryOperatorKind.Greater:
                    {
                        emittion.AddCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AddCommand($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".greater");
                        emittion.AddCommand($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {name} {Vars} 0");
                        emittion.AddCommand($"tag @e[tag=.this] remove .this");
                        break;
                    }
                case BoundBinaryOperatorKind.Less:
                    {
                        emittion.AddCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AddCommand($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".less");
                        emittion.AddCommand($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {name} {Vars} 0");
                        emittion.AddCommand($"tag @e[tag=.this] remove .this");
                        break;
                    }
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    {
                        emittion.AddCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AddCommand($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".greater");
                        emittion.AddCommand($"tag @e[tag=.this] remove .this");
                        break;
                    }
                case BoundBinaryOperatorKind.LessOrEquals:
                    {
                        emittion.AddCommand($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AddCommand($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".less");
                        emittion.AddCommand($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {name} {Vars} 1");
                        emittion.AddCommand($"tag @e[tag=.this] remove .this");
                        break;
                    }
            }

            EmitCleanUp(temp, left.Type, emittion);
            if (left.Type != right.Type)
                EmitCleanUp(temp, right.Type, emittion);

            EmitMacroCleanUp(emittion);
        }

        private void EmitIntBinaryOperation(string name, MinecraftFunction emittion, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int current)
        {
            var leftName = EmitAssignmentExpression(name, left, emittion, current);
            var rightName = string.Empty;

            if (right is BoundLiteralExpression l)
            {
                if (operation == BoundBinaryOperatorKind.Addition)
                {
                    var command1 = $"scoreboard players add {leftName} {Vars} {l.Value}";
                    emittion.AddCommand(command1);
                    return;
                }
                else if (operation == BoundBinaryOperatorKind.Subtraction)
                {
                    var command1 = $"scoreboard players remove {leftName} {Vars} {l.Value}";
                    emittion.AddCommand(command1);
                    return;
                }
                else
                {
                    rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1);
                    EmitCleanUp(rightName, left.Type, emittion);
                }
            }
            else if (right is BoundVariableExpression v)
            {
                rightName = _nameTranslator.GetVariableName(v.Variable);
            }
            else
            {
                rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1);
                EmitCleanUp(rightName, left.Type, emittion);
            }

            var operationSign = operation switch
            {
                BoundBinaryOperatorKind.Addition => "+=",
                BoundBinaryOperatorKind.Subtraction => "-=",
                BoundBinaryOperatorKind.Multiplication => "*=",
                BoundBinaryOperatorKind.Division => "/=",
                _ => "="
            };
            var command = $"scoreboard players operation {leftName} {Vars} {operationSign} {rightName} {Vars}";
            emittion.AddCommand(command);
        }

        private void EmitFloatingPointBinaryOperation(string name, MinecraftFunction emittion, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind kind, int current)
        {
            var leftName = EmitAssignmentExpression(name, left, emittion, current);
            var rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1);
            var storage = _nameTranslator.GetStorage(left.Type);

            if (kind != BoundBinaryOperatorKind.Subtraction)
            {
                emittion.AddCommand($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a set from storage {storage} \"{leftName}\"");
                emittion.AddCommand($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.b set from storage {_nameTranslator.GetStorage(right.Type)} \"{rightName}\"");
            }

            MinecraftFunction macro;
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
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Subtract, out bool isCreated);
                        var stringStorage = _nameTranslator.GetStorage(TypeSymbol.String);
                        var sub = macro.SubFunctions?.FirstOrDefault(n => n.Name == $"{macro.Name}_if_minus");

                        if (sub == null)
                        {
                            sub = macro.CreateSubNamed("if_minus");
                            sub.AddCommand($"data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 1");
                            sub.AddCommand($"data modify storage {stringStorage} \"**last\" set string storage {stringStorage} **macros.a -1");
                            sub.AddCommand($"execute if data storage {stringStorage} {{ \"**last\" : \"d\" }} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 0 -1");
                            sub.AddCommand($"execute if data storage {stringStorage} {{ \"**last\" : \"f\" }} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 0 -1");
                            sub.AddCommand($"data modify storage {stringStorage} **macros.polarity set value \"\"");
                        }

                        if (isCreated)
                            macro.AddMacro($"execute positioned ~ $(b) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(polarity)$(a) {DEBUG_CHUNK_Z}");

                        emittion.AddCommand($"data modify storage {stringStorage} **macros.b set from storage {storage} \"{leftName}\"");
                        emittion.AddCommand($"data modify storage {stringStorage} **macros.a set from storage {_nameTranslator.GetStorage(right.Type)} \"{rightName}\"");
                        emittion.AddCommand($"data modify storage {stringStorage} \"**pol\" set from storage {_nameTranslator.GetStorage(right.Type)} \"{rightName}\"");
                        emittion.AddCommand($"data modify storage {stringStorage} \"**pol\" set string storage {stringStorage} \"**pol\" 0 1");

                        emittion.AddCommand($"execute if data storage {stringStorage} {{ \"**pol\" : \"-\" }} run function {_nameTranslator.GetCallLink(sub)}");
                        emittion.AddCommand($"execute unless data storage {stringStorage} {{ \"**pol\" : \"-\" }} run data modify storage {stringStorage} **macros.polarity set value \"-\"");

                        EmitDoubleConversion("**macros.a", "**macros.a", TypeSymbol.String, emittion, stringStorage);
                        EmitCleanUp("**last", TypeSymbol.String, emittion);
                        EmitCleanUp("**pol", TypeSymbol.String, emittion);
                        break;
                    }
                case BoundBinaryOperatorKind.Multiplication:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Multiply, out bool isCreated);

                        if (isCreated)
                        {
                            macro.AddMacro($"data modify storage {_nameTranslator.MainStorage} {RETURN_TEMP_NAME} set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f]");
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

            emittion.AddCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} **macros");

            if (kind == BoundBinaryOperatorKind.Addition || kind == BoundBinaryOperatorKind.Subtraction)
            {
                emittion.AddCommand($"data modify storage {storage} {name} set from entity {_nameTranslator.MathEntity1.ToString()} Pos[1]");
                emittion.AddCommand($"tp {entity} {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z}");

                if (left.Type == TypeSymbol.Float)
                    EmitFloatConversion(name, name, TypeSymbol.Float, emittion);

            }
            else if (kind == BoundBinaryOperatorKind.Multiplication)
            {
                emittion.AddCommand($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} {RETURN_TEMP_NAME}[-1] set from entity {entity} transformation.translation[0]");
                emittion.AddCommand($"data modify entity {entity} transformation set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} {RETURN_TEMP_NAME}");
                emittion.AddCommand($"data modify storage {storage} {name} set from entity {entity} transformation.translation[0]");
                EmitCleanUp(RETURN_TEMP_NAME, TypeSymbol.String, emittion);

                if (left.Type == TypeSymbol.Double)
                    EmitDoubleConversion(name, name, TypeSymbol.Double, emittion);
            }
            else if (kind == BoundBinaryOperatorKind.Division)
            {
                emittion.AddCommand($"data modify storage {storage} {name} set from entity {entity} transformation.translation[0]");

                if (left.Type == TypeSymbol.Double)
                    EmitDoubleConversion(name, name, TypeSymbol.Double, emittion);
            }

            EmitMacroCleanUp(emittion);
        }


        private void EmitCallExpressionAssignment(string name, BoundCallExpression call, MinecraftFunction emittion, int current)
        {
            //The return value via a temp variable also works for int and bool, but since
            //Mojang's added /return why not use it instead

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

            emittion.AddComment($"Assigning return value of {call.Function.Name} to \"{name}\"");

            if (call.Function.ReturnType == TypeSymbol.Int || call.Function.ReturnType == TypeSymbol.Bool || call.Function.ReturnType is EnumSymbol)
            {
                var isBuiltIt = TryEmitBuiltInFunction(name, call, emittion, current);

                if (!isBuiltIt)
                {
                    var setParameters = EmitFunctionParametersAssignment(call.Function.Parameters, call.Arguments, emittion);
                    var command = $"execute store result score {name} {Vars} run function {_nameTranslator.GetCallLink(call.Function)}";
                    emittion.AddCommand(command);
                    EmitFunctionParameterCleanUp(setParameters, emittion);
                }
            }
            else
            {
                EmitCallExpression(name, call, emittion, current);
                var command2 = $"data modify storage {_nameTranslator.GetStorage(call.Type)} \"{name}\" set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{RETURN_TEMP_NAME}\"";
                emittion.AddCommand(command2);
            }
        }

        private Dictionary<ParameterSymbol, string> EmitFunctionParametersAssignment(ImmutableArray<ParameterSymbol> parameters, ImmutableArray<BoundExpression> arguments, MinecraftFunction emittion)
        {
            var setNames = new Dictionary<ParameterSymbol, string>();

            for (int i = 0; i < arguments.Count(); i++)
            {
                var argument = arguments[i];
                var parameter = parameters[i];
                var paramName = EmitAssignmentExpression(parameter, argument, emittion, 0);
                setNames.Add(parameter, paramName);
            }
            return setNames;
        }

        private void EmitFunctionParameterCleanUp(Dictionary<ParameterSymbol, string> parameters, MinecraftFunction emittion)
        {
            foreach (var parameter in parameters.Keys)
            {
                var name = parameters[parameter];
                EmitCleanUp(name, parameter.Type, emittion);
            }
        }

        private void EmitConversionExpressionAssignment(string name, BoundConversionExpression conversion, MinecraftFunction emittion, int current)
        {
            //to int -> scoreboard players operation
            //to string -> copy to storage, than copy with data modify ... string
            //to object -> data modify storage

            emittion.AddComment($"Assigning a conversion from {conversion.Expression.Type} to {conversion.Type} to variable \"{name}\"");
            var resultType = conversion.Type;
            var sourceType = conversion.Expression.Type;

            if (sourceType is NamedTypeSymbol && resultType is NamedTypeSymbol)
            {
                EmitAssignmentExpression(name, conversion.Expression, emittion, current);
            }
            else
            {
                var tempName = EmitAssignmentToTemp(conversion.Expression, emittion, current);
                if (resultType == TypeSymbol.String)
                {
                    var stringsStorage = _nameTranslator.GetStorage(TypeSymbol.String);
                    var tempPath = "TEMP.*temp1";

                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        var command1 = $"execute store result storage {stringsStorage} \"{tempPath}\" int 1 run scoreboard players get {tempName} {Vars}";
                        var command2 = $"data modify storage {stringsStorage} \"{name}\" set string storage {stringsStorage} \"{tempPath}\"";
                        emittion.AddCommand(command1);
                        emittion.AddCommand(command2);
                        EmitCleanUp(tempPath, resultType, emittion);
                    }
                    else if (sourceType is EnumSymbol enumSymbol)
                    {
                        if (enumSymbol.IsIntEnum)
                        {
                            foreach (var enumMember in enumSymbol.Members)
                            {
                                var intMember = (IntEnumMemberSymbol)enumMember;
                                var command = $"execute if score {tempName} {Vars} matches {intMember.UnderlyingValue} run data modify storage {stringsStorage} \"{name}\" set value \"{enumMember.Name}\"";
                                emittion.AddCommand(command);
                            }
                        }
                        else
                        {
                            var enumStorage = _nameTranslator.GetStorage(enumSymbol);
                            var command = $"data modify storage {stringsStorage} \"{name}\" set from storage {enumStorage} \"{tempName}\"";
                            emittion.AddCommand(command);
                        }
                    }
                }
                if (resultType == TypeSymbol.Float)
                {
                    EmitFloatConversion(name, tempName, sourceType, emittion);
                }
                if (resultType == TypeSymbol.Double)
                {
                    EmitDoubleConversion(name, tempName, sourceType, emittion);
                }
                if (resultType == TypeSymbol.Object)
                {
                    if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                    {
                        emittion.AddCommand($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.Object)} \"{name}\" int 1 run scoreboard players get {tempName} {Vars}");
                    }
                    else
                    {
                        emittion.AddCommand($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Object)} \"{name}\" set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{tempName}\"");
                    }
                }
                EmitCleanUp(tempName, sourceType, emittion);
            }
        }

        private void EmitFloatConversion(string name, string otherName, TypeSymbol sourceType, MinecraftFunction emittion)
        {
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToFloat, out bool isCreated);
            var storage = _nameTranslator.GetStorage(TypeSymbol.Float);

            if (isCreated)
                macro.AddMacro($"data modify storage {storage} {RETURN_TEMP_NAME} set value $(a)f");

            if (sourceType == TypeSymbol.Int)
                emittion.AddCommand($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a int 1 run scoreboard players get {otherName} {Vars}");
            else
                emittion.AddCommand($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a set from storage {_nameTranslator.GetStorage(sourceType)} \"{otherName}\"");

            emittion.AddCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");
            emittion.AddCommand($"data modify storage {storage} {name} set from storage {storage} {RETURN_TEMP_NAME}");
            EmitMacroCleanUp(emittion);
        }

        private void EmitDoubleConversion(string name, string sourceName, TypeSymbol sourceType, MinecraftFunction emittion, string? emittionStorage = null)
        {
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToDouble, out bool isCreated);
            var storage = _nameTranslator.GetStorage(TypeSymbol.Double);

            if (isCreated)
                macro.AddMacro($"data modify storage {storage} {RETURN_TEMP_NAME} set value $(a)d");

            if (sourceType == TypeSymbol.Int)
                emittion.AddCommand($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a int 1 run scoreboard players get {sourceName} {Vars}");
            else
                emittion.AddCommand($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a set from storage {_nameTranslator.GetStorage(sourceType)} \"{sourceName}\"");

            var resultStorage = emittionStorage ?? storage;
            emittion.AddCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");
            emittion.AddCommand($"data modify storage {resultStorage} {name} set from storage {storage} {RETURN_TEMP_NAME}");
            EmitMacroCleanUp(emittion);
        }

        private void EmitCleanUp(string name, TypeSymbol type, MinecraftFunction emittion)
        {
            string command;

            if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol e && e.IsIntEnum)
                command = $"scoreboard players reset {name} {Vars}";
            else
                command = $"data remove storage {_nameTranslator.GetStorage(type)} \"{name}\"";

            emittion.AddCommand(command, isCleanUp: true);
        }

        private void EmitMacroCleanUp(MinecraftFunction emittion) => EmitCleanUp("**macros", TypeSymbol.String, emittion);

        private string EmitAssignmentToTemp(string tempName, BoundExpression expression, MinecraftFunction emittion, int index, bool addDot = true)
        {
            var varName = $"{(addDot ? "." : string.Empty)}{tempName}{index}";
            var resultName = EmitAssignmentExpression(varName, expression, emittion, index);
            return resultName;
        }

        private string EmitAssignmentToTemp(BoundExpression expression, MinecraftFunction emittion, int index) => EmitAssignmentToTemp(TEMP, expression, emittion, index);

        private bool IsStorageType(TypeSymbol type)
        {
            return type == TypeSymbol.String ||
                   type == TypeSymbol.Object ||
                   type == TypeSymbol.Float ||
                   type == TypeSymbol.Double ||
                   type is EnumSymbol e && !e.IsIntEnum;
        }

        private MinecraftFunction GetOrCreateBuiltIn(FunctionSymbol function, out bool isCreated)
        {
            MinecraftFunction emittion;
            isCreated = !_usedBuiltIn.ContainsKey(function);

            if (isCreated)
            {
                emittion = new MinecraftFunction(function.Name, function, null);
                _usedBuiltIn.Add(function, emittion);
            }
            else
                emittion = _usedBuiltIn[function];

            return emittion;
        }

        public bool TryEmitBuiltInFieldGetter(string outputName, BoundFieldAccessExpression right, MinecraftFunction emittion, int current)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(right.Field))
            {
                var command = $"execute store result score {outputName} {Vars} run gamerule {right.Field.Name}";
                emittion.AddCommand(command);
                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == right.Field)
            {
                var command = $"execute store result score {outputName} {Vars} run difficulty";
                emittion.AddCommand(command);
                return true;
            }
            return false;
        }

        public bool TryEmitBuiltInFieldAssignment(FieldSymbol field, BoundExpression right, MinecraftFunction emittion, int current, out string? tempName)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(field))
            {
                tempName = EmitGameruleAssignment(field, right, emittion, current);
                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == field)
            {
                tempName = EmitDifficultyAssignment(field, right, emittion, current);
                return true;
            }
            tempName = null;
            return false;
        }

        private string EmitGameruleAssignment(FieldSymbol field, BoundExpression right, MinecraftFunction emittion, int current)
        {
            //1. Evaluate right to temp
            //2. If is bool, generate two conditions
            //3. If it's an int, generate a macro

            var rightName = EmitAssignmentToTemp(right, emittion, current);

            if (field.Type == TypeSymbol.Bool)
            {
                var command1 = $"execute if score {rightName} {Vars} matches 1 run gamerule {field.Name} true";
                var command2 = $"execute if score {rightName} {Vars} matches 0 run gamerule {field.Name} false";
                emittion.AddCommand(command1);
                emittion.AddCommand(command2);
            }
            else
            {
                var macroFunctionSymbol = BuiltInNamespace.Minecraft.General.Gamerules.SetGamerule;
                var macro = GetOrCreateBuiltIn(macroFunctionSymbol, out bool isCreated);

                var command1 = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.rule set value \"{field.Name}\"";
                var command2 = $"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.value int 1 run scoreboard players get {rightName} {Vars}";
                var command3 = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";

                if (isCreated)
                    macro.AddMacro("gamerule $(rule) $(value)");

                emittion.AddCommand(command1);
                emittion.AddCommand(command2);
                emittion.AddCommand(command3);
                EmitMacroCleanUp(emittion);
            }

            EmitCleanUp(rightName, right.Type, emittion);
            return rightName;
        }

        private string EmitDifficultyAssignment(FieldSymbol field, BoundExpression right, MinecraftFunction emittion, int current)
        {
            if (right is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
            {
                var command = $"difficulty {em.Name.ToLower()}";
                emittion.AddCommand(command);
                return string.Empty;
            }

            var rightName = EmitAssignmentToTemp(right, emittion, current);

            foreach (var enumMember in BuiltInNamespace.Minecraft.General.Difficulty.Members)
            {
                var intMember = (IntEnumMemberSymbol)enumMember;
                var command = $"execute if score {rightName} {Vars} matches {intMember.UnderlyingValue} run difficulty {enumMember.Name.ToLower()}";
                emittion.AddCommand(command);
            }

            EmitCleanUp(rightName, right.Type, emittion);
            return rightName;
        }

        public bool TryEmitBuiltInFunction(string? varName, BoundCallExpression call, MinecraftFunction emittion, int current)
        {
            if (call.Function == BuiltInNamespace.Minecraft.General.RunCommand)
            {
                EmitRunCommand(call, emittion);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackEnable)
            {
                EmitDatapackEnable(call, emittion);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.DatapackDisable)
            {
                EmitDatapackDisable(call, emittion);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.SetDatapackEnabled)
            {
                EmitSetDatapackEnabled(call, emittion, current);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeather)
            {
                EmitSetWeather(call, emittion, current);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForTicks)
            {
                EmitSetWeatherForTicks(call, emittion, current);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForDays)
            {
                EmitSetWeatherForDays(call, emittion, current);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.Weather.SetWeatherForSeconds)
            {
                EmitSetWeatherForSeconds(call, emittion, current);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Say)
            {
                EmitSay(call, emittion);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Print)
            {
                EmitPrint(call, emittion);
                return true;
            }

            //Non void functions
            if (varName == null)
                return false;

            if (call.Function == BuiltInNamespace.Minecraft.General.GetDatapackCount)
            {
                EmitGetDatapackCount(varName, call, emittion);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetEnabledDatapackCount)
            {
                EmitGetDatapackCount(varName, call, emittion, true);
                return true;
            }
            if (call.Function == BuiltInNamespace.Minecraft.General.GetAvailableDatapackCount)
            {
                EmitGetDatapackCount(varName, call, emittion, false, true);
                return true;
            }
            return false;
        }

        private void EmitRunCommand(BoundCallExpression call, MinecraftFunction emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.command", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AddMacro("$(command)");

            emittion.AddCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");
            EmitMacroCleanUp(emittion);
        }

        private void EmitDatapackEnable(BoundCallExpression call, MinecraftFunction emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.pack", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AddMacro($"datapack enable \"file/$(pack)\"");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";
            emittion.AddCommand(command);
            EmitMacroCleanUp(emittion);
        }

        private void EmitDatapackDisable(BoundCallExpression call, MinecraftFunction emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.pack", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AddMacro($"datapack disable \"file/$(pack)\"");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";
            emittion.AddCommand(command);
            EmitMacroCleanUp(emittion);
        }

        private void EmitSetDatapackEnabled(BoundCallExpression call, MinecraftFunction emittion, int current)
        {
            var pack = call.Arguments[0];
            var value = call.Arguments[1];

            var packName = EmitAssignmentExpression("**macros.pack", pack, emittion, current);
            var valueName = EmitAssignmentToTemp(TEMP, value, emittion, current, false);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
            {
                macro.AddMacro($"execute if score {valueName} {Vars} matches 1 run return run datapack enable \"file/$(pack)\"");
                macro.AddMacro($"datapack disable \"file/$(pack)\"");
            }

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";
            emittion.AddCommand(command);

            EmitCleanUp(valueName, value.Type, emittion);
            EmitMacroCleanUp(emittion);
        }

        private void EmitGetDatapackCount(string name, BoundCallExpression call, MinecraftFunction emittion, bool countEnabled = false, bool countAvailable = false)
        {
            string filter = string.Empty;
            if (countEnabled)
                filter = "enabled";
            else
                filter = "available";

            var command = $"execute store result score {name} {Vars} run datapack list {filter}";
            emittion.AddCommand(command);
        }

        private void EmitSetWeather(BoundCallExpression call, MinecraftFunction emittion, int current, string? timeUnits = null)
        {
            void EmitNonMacroNonConstantTypeCheck(BoundExpression weatherType, MinecraftFunction emittion, int current, int time = 0, string? timeUnits = null)
            {
                var right = EmitAssignmentToTemp("type", weatherType, emittion, current);

                foreach (var enumMember in BuiltInNamespace.Minecraft.General.Weather.Weather.Members)
                {
                    var intMember = (IntEnumMemberSymbol)enumMember;

                    string command;
                    if (timeUnits == null)
                        command = $"execute if score {right} {Vars} matches {intMember.UnderlyingValue} run weather {enumMember.Name.ToLower()}";
                    else
                        command = $"execute if score {right} {Vars} matches {intMember.UnderlyingValue} run weather {enumMember.Name.ToLower()} {time}{timeUnits}";

                    emittion.AddCommand(command);
                }

                EmitCleanUp(right, weatherType.Type, emittion);
            }

            var weatherType = call.Arguments[0];

            if (call.Arguments.Length > 1)
            {
                //Time specified
                if (call.Arguments[1] is BoundLiteralExpression l)
                {
                    if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                    {
                        emittion.AddCommand($"weather {em.Name.ToLower()} {l.Value}{timeUnits}");
                        return;
                    }
                    else
                    {
                        var time = (int)l.Value;
                        EmitNonMacroNonConstantTypeCheck(weatherType, emittion, current, time, timeUnits);
                    }
                }
                else
                {
                    var type = EmitAssignmentExpression("**macros.type", weatherType, emittion, current);
                    var duration = EmitAssignmentToTemp("dur", call.Arguments[1], emittion, current);
                    var macro = GetOrCreateBuiltIn(BuiltInNamespace.Minecraft.General.Weather.SetWeather, out bool isCreated);

                    emittion.AddCommand($"execute store result storage {_nameTranslator.MainStorage} **macros.duration int 1 run scoreboard players get {duration} {Vars}");
                    emittion.AddCommand($"data modify storage {_nameTranslator.MainStorage} **macros.tu set value \"{timeUnits}\"");
                    emittion.AddCommand($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.MainStorage} **macros");

                    if (isCreated)
                        macro.AddMacro($"weather $(type) $(duration)(tu)");

                    EmitCleanUp(duration, TypeSymbol.Int, emittion);
                    EmitMacroCleanUp(emittion);
                }
            }
            else
            {
                //Don't have time specified
                if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                {
                    emittion.AddCommand($"weather {em.Name.ToLower()}");
                }
                else
                {
                    EmitNonMacroNonConstantTypeCheck(weatherType, emittion, current);
                }
            }
        }

        private void EmitSetWeatherForTicks(BoundCallExpression call, MinecraftFunction emittion, int current)
            => EmitSetWeather(call, emittion, current, "t");

        private void EmitSetWeatherForSeconds(BoundCallExpression call, MinecraftFunction emittion, int current)
            => EmitSetWeather(call, emittion, current, "s");

        private void EmitSetWeatherForDays(BoundCallExpression call, MinecraftFunction emittion, int current)
            => EmitSetWeather(call, emittion, current, "d");

        private void EmitSay(BoundCallExpression call, MinecraftFunction emittion) => EmitPrint(call, emittion);

        private void EmitPrint(BoundCallExpression call, MinecraftFunction emittion)
        {
            var argument = call.Arguments[0];
            var command = string.Empty;

            if (argument is BoundLiteralExpression literal)
            {
                command = "tellraw @a {\"text\":\"" + literal.Value + "\"}";
            }
            else if (argument is BoundVariableExpression variable)
            {
                var varName = _nameTranslator.GetVariableName(variable.Variable);
                command = $"tellraw @a {{\"storage\":\"{_nameTranslator.GetStorage(TypeSymbol.String)}\",\"nbt\":\"\\\"{varName}\\\"\"}}";
            }
            else
            {
                var tempName = EmitAssignmentToTemp(TEMP, argument, emittion, 0, false);
                command = $"tellraw @a {{\"storage\":\"{_nameTranslator.GetStorage(TypeSymbol.String)}\",\"nbt\":\"\\\"{tempName}\\\"\"}}";
                EmitCleanUp(tempName, argument.Type, emittion);
            }

            emittion.AddCommand(command);
        }
    }
}