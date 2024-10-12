using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Emit.NameTranslation;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public partial class BuiltInFunctionEmitter { }

        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;
        private readonly EmittionNameTranslator _nameTranslator;

        private readonly FunctionEmittion _initFunction;
        private readonly FunctionEmittion _tickFunction;
        private readonly Dictionary<FunctionSymbol, FunctionEmittion> _usedBuiltIn = new Dictionary<FunctionSymbol, FunctionEmittion>();
        
        private string? _contextName = null;
        private string TEMP => EmittionNameTranslator.TEMP;
        private string RETURN_TEMP_NAME => EmittionNameTranslator.RETURN_TEMP_NAME;
        private string DEBUG_CHUNK_X => EmittionNameTranslator.DEBUG_CHUNK_X;
        private string DEBUG_CHUNK_Z => EmittionNameTranslator.DEBUG_CHUNK_Z;

        private string Vars => _nameTranslator.Vars;
        private string Const => _nameTranslator.Const;

        public DatapackEmitter(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _configuration = configuration;
            _nameTranslator = new EmittionNameTranslator(configuration.RootNamespace);

            _initFunction = FunctionEmittion.Init(program.GlobalNamespace);
            _tickFunction = FunctionEmittion.Tick(program.GlobalNamespace);
            AddInitializationCommands();
        }

        private void AddInitializationCommands()
        {
            _initFunction.AppendComment("Blaze setup");
            _initFunction.AppendLine($"scoreboard objectives add {Vars} dummy");
            _initFunction.AppendLine($"scoreboard objectives add {Const} dummy");
            _initFunction.AppendLine($"scoreboard players set *-1 {Const} -1");

            //Debug chunk setup
            _initFunction.AppendLine();
            _initFunction.AppendLine($"forceload add {DEBUG_CHUNK_X} {DEBUG_CHUNK_Z}");
            _initFunction.AppendLine($"kill @e[tag=debug,tag=blz]");
            _initFunction.AppendLine($"summon item_display {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z} {{Tags:[\"blz\",\"debug\", \"first\"], UUID:{_nameTranslator.MathEntity1.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:1,less:0}}}}}}}}");
            _initFunction.AppendLine($"summon item_display {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z} {{Tags:[\"blz\",\"debug\", \"second\"], UUID:{_nameTranslator.MathEntity2.TagValue}, item:{{ id:\"stone_button\",Count:1b,components:{{\"minecraft:custom_data\":{{greater:0,less:1}}}}}}}}");
        }

        private FunctionEmittion GetOrCreateBuiltIn(FunctionSymbol function, out bool isCreated)
        {
            FunctionEmittion emittion;
            isCreated = !_usedBuiltIn.ContainsKey(function);

            if (isCreated)
            {
                emittion = FunctionEmittion.FromSymbol(function);
                _usedBuiltIn.Add(function, emittion);
            }
            else
                emittion = _usedBuiltIn[function];

            return emittion;
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CompilationConfiguration? configuration)
        {
            if (program.Diagnostics.Any() || configuration == null)
                return program.Diagnostics;

            var emitter = new DatapackEmitter(program, configuration);
            emitter.BuildPacks();

            return program.Diagnostics;
        }

        private void BuildPacks()
        {
            var functionNamespaceEmittionBuilder = ImmutableArray.CreateBuilder<FunctionNamespaceEmittion>();

            foreach (var ns in _program.Namespaces)
            {
                var namespaceSymbol = ns.Key;
                var boundNamespace = ns.Value;

                var namespaceEmittion = EmitFunctionNamespace(namespaceSymbol, boundNamespace);
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

            var datapack = new Datapack(_configuration, functionNamespaceEmittionBuilder.ToImmutable(), _initFunction, _tickFunction);
            datapack.Build();
        }

        private FunctionNamespaceEmittion? EmitBuiltInNamespace(NamespaceSymbol ns)
        {
            //We do this so that we do not generate unused functions and folders

            ImmutableArray<FunctionEmittion>.Builder? functionsBuilder = null;
            ImmutableArray<FunctionNamespaceEmittion>.Builder? childrenBuilder = null;

            foreach (var function in ns.Functions)
            {
                if (_usedBuiltIn.ContainsKey(function))
                {
                    if (functionsBuilder == null)
                        functionsBuilder = ImmutableArray.CreateBuilder<FunctionEmittion>();

                    var emittion = _usedBuiltIn[function];
                    functionsBuilder.Add(emittion);
                }
            }

            foreach (var child in ns.NestedNamespaces)
            {
                var emittion = EmitBuiltInNamespace(child);
                if (emittion != null)
                {
                    if (childrenBuilder == null)
                        childrenBuilder = ImmutableArray.CreateBuilder<FunctionNamespaceEmittion>();
                    childrenBuilder.Add(emittion);
                }
            }

            FunctionNamespaceEmittion? result = null;

            if (functionsBuilder != null || childrenBuilder != null)
            {
                var functions = functionsBuilder == null ? ImmutableArray<FunctionEmittion>.Empty : functionsBuilder.ToImmutable();
                var children = childrenBuilder == null ? ImmutableArray<FunctionNamespaceEmittion>.Empty : childrenBuilder.ToImmutable();
                result = new FunctionNamespaceEmittion(ns.Name, children, functions);
            }
            return result;
        }

        private FunctionNamespaceEmittion EmitFunctionNamespace(NamespaceSymbol symbol, BoundNamespace boundNamespace)
        {
            var functionsBuilder = ImmutableArray.CreateBuilder<FunctionEmittion>();
            var childrenBuilder = ImmutableArray.CreateBuilder<FunctionNamespaceEmittion>();

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
                //TODO: Pass the namespace in the function emittion
                var functionEmittion = EmitFunction(function.Key, function.Value);
                functionsBuilder.Add(functionEmittion);
            }

            foreach (var child in boundNamespace.Children)
            {
                var childEmittion = EmitFunctionNamespace(child.Key, child.Value);
                childrenBuilder.Add(childEmittion);
            }

            var namespaceEmittion = new FunctionNamespaceEmittion(symbol.Name, childrenBuilder.ToImmutable(), functionsBuilder.ToImmutable());
            return namespaceEmittion;
        }

        private FunctionEmittion EmitFunction(FunctionSymbol function, BoundStatement bodyBlock)
        {
            var emittion = FunctionEmittion.FromSymbol(function);

            if (function.IsLoad)
                _initFunction.AppendLine($"function {_nameTranslator.GetCallLink(function)}");

            if (function.IsTick)
                _tickFunction.AppendLine($"function {_nameTranslator.GetCallLink(function)}");

            EmitStatement(bodyBlock, emittion);
            return emittion;
        }

        private void EmitStatement(BoundStatement node, FunctionEmittion emittion)
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
                    EvaluateIfStatement((BoundIfStatement)node, emittion);
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

        private void EmitBlockStatement(BoundBlockStatement node, FunctionEmittion emittion)
        {
            foreach (BoundStatement statement in node.Statements)
                EmitStatement(statement, emittion);
        }
        
        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement node, FunctionEmittion emittion)
        {
            var name = _nameTranslator.GetVariableName(node.Variable);
            EmitAssignmentExpression(name, node.Initializer, emittion, 0);
        }

        private void EvaluateIfStatement(BoundIfStatement node, FunctionEmittion emittion)
        {
            //Emit condition into <.temp>
            //execute if <.temp> run subfunction
            //else generate a sub function and run it instead
            //if there is an else clause generate another sub with the else body

            var subFunction = FunctionEmittion.CreateSub(emittion, SubFunctionKind.If);
            EmitStatement(node.Body, subFunction);

            var tempName = EmitAssignmentToTemp(node.Condition, emittion, 0);
            var callClauseCommand = $"execute if score {tempName} {Vars} matches 1 run function {_nameTranslator.GetCallLink(subFunction)}";

            emittion.AppendLine(callClauseCommand);

            if (node.ElseBody != null)
            {
                var elseSubFunction = FunctionEmittion.CreateSub(emittion, SubFunctionKind.Else);
                EmitStatement(node.ElseBody, elseSubFunction);
                var elseCallClauseCommand = $"execute if score {tempName} {Vars} matches 0 run function {_nameTranslator.GetCallLink(elseSubFunction)}";

                emittion.AppendLine(elseCallClauseCommand);
            }
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
        }

        private void EvaluateWhileStatement(BoundWhileStatement node, FunctionEmittion emittion)
        {
            //Main:
            //Call sub function
            //
            //Generate body sub function:
            //Emit condition into <.temp>
            //execute if <.temp> run return 0
            //body

            var subFunction = FunctionEmittion.CreateSub(emittion, SubFunctionKind.Loop);
            var callCommand = $"function {_nameTranslator.GetCallLink(subFunction)}";

            var tempName = EmitAssignmentToTemp(node.Condition, subFunction, 0);
            var breakClauseCommand = $"execute if score {tempName} {Vars} matches 0 run return 0";
            subFunction.AppendLine(breakClauseCommand);
            subFunction.AppendLine();

            EmitStatement(node.Body, subFunction);
            subFunction.AppendLine();
            subFunction.AppendLine(callCommand);
            
            emittion.AppendLine(callCommand);
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
        }

        private void EvaluateDoWhileStatement(BoundDoWhileStatement node, FunctionEmittion emittion)
        {
            //Main:
            //Call sub function
            //
            //Generate body sub function:
            //body
            //Emit condition into <.temp>
            //execute if <.temp> run function <subfunction>

            var subFunction = FunctionEmittion.CreateSub(emittion, SubFunctionKind.Loop);
            var callCommand = $"function {_nameTranslator.GetCallLink(subFunction)}";

            EmitStatement(node.Body, subFunction);

            var tempName = EmitAssignmentToTemp(node.Condition, subFunction, 0);
            var loopClauseCommand = $"execute if score {tempName} {Vars} matches 1 run {callCommand}";
            subFunction.AppendLine(loopClauseCommand);
            subFunction.AppendLine();

            emittion.AppendLine(callCommand);
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
        }

        private void EmitContinueStatement(BoundContinueStatement node, FunctionEmittion emittion)
        {
            throw new NotImplementedException();
        }

        private void EmitBreakStatement(BoundBreakStatement node, FunctionEmittion emittion)
        {
            throw new NotImplementedException();
        }

        private void EmitReturnStatement(BoundReturnStatement node, FunctionEmittion emittion)
        {
            //Emit cleanup before we break the function
            //Assign the return value to <return.value>
            //If the return value is an integer or a bool, return it

            void EmitCleanUp()
            {
                emittion.AppendComment("Clean up before break");
                emittion.Append(emittion.CleanUp);
                emittion.AppendLine();
            }

            var returnExpression = node.Expression;
            if (returnExpression == null)
            {
                EmitCleanUp();
                emittion.AppendLine("return 0");
                return;
            }
            
            var desiredReturnName = (returnExpression.Type is NamedTypeSymbol && _contextName != null) ? _contextName : EmittionNameTranslator.RETURN_TEMP_NAME;
            var returnName = EmitAssignmentToTemp(desiredReturnName, returnExpression, emittion, 0, false);
            EmitCleanUp();

            if (returnExpression.Type == TypeSymbol.Int || returnExpression.Type == TypeSymbol.Bool || returnExpression.Type is EnumSymbol e && e.IsIntEnum)
            {
                var returnCommand = $"return run scoreboard players get {returnName} {Vars}";
                emittion.AppendLine(returnCommand);
            }
            else
            {
                emittion.AppendLine("return 0");
            }
        }

        private void EmitExpressionStatement(BoundExpressionStatement node, FunctionEmittion emittion)
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

        private void EmitCallExpression(string? name, BoundCallExpression call, FunctionEmittion emittion, int current)
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
                emittion.AppendLine(command);

                EmitFunctionParameterCleanUp(setNames, emittion);
            }
        }

        private void EmitNopStatement(FunctionEmittion emittion)
        {
            emittion.AppendLine("tellraw @a {\"text\":\"Nop statement in program\", \"color\":\"red\"}");
        }

        private string EmitAssignmentExpression(BoundAssignmentExpression assignment, FunctionEmittion emittion, int current) 
            => EmitAssignmentExpression(assignment.Left, assignment.Right, emittion, current);

        private string EmitAssignmentExpression(BoundExpression left, BoundExpression right, FunctionEmittion emittion, int current)
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

            var leftName = GetNameOfAssignableExpression(left);
            return EmitAssignmentExpression(leftName, right, emittion, current);
        }
        
        private string EmitAssignmentExpression(VariableSymbol variable, BoundExpression right, FunctionEmittion emittion, int current)
            => EmitAssignmentExpression(_nameTranslator.GetVariableName(variable), right, emittion, current);
        
        private string EmitAssignmentExpression(string name, BoundExpression right, FunctionEmittion emittion, int current)
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
            else if (right is BoundFieldAccessExpression fieldExpression)
            {
                if (!TryEmitBuiltInFieldGetter(name, fieldExpression, emittion, current))
                {
                    var otherName = GetNameOfAssignableExpression(fieldExpression);
                    EmitVariableAssignment(name, otherName, right.Type, emittion);
                }
            }
            else
            {
                emittion.AppendLine($"#Unsupported expression type {right.Kind}");
            }
            return name;
        }

        private string GetNameOfAssignableExpression(BoundExpression left)
        {
            if (left is BoundVariableExpression v)
            {
                return _nameTranslator.GetVariableName(v.Variable);
            }
            else if (left is BoundFieldAccessExpression fa)
            {
                var fieldName = fa.Field.Name;
                var leftAssociativeOrder = new Stack<BoundExpression>();

                var previous = fa.Instance;
                while (true)
                {
                    leftAssociativeOrder.Push(previous);

                    if (previous is BoundFieldAccessExpression fieldAccess)
                        previous = fieldAccess.Instance;
                    else if (previous is BoundCallExpression call)
                        previous = call.Identifier;
                    else if (previous is BoundMethodAccessExpression methodAccess)
                        previous = methodAccess.Instance;
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
                        //TODO: add Field initialization in init function

                        nameBuilder.Append(_nameTranslator.GetNamespaceFieldPath(namespaceExpression.Namespace));
                    }
                    else if (current is BoundFieldAccessExpression fieldAccess)
                    {
                        nameBuilder.Append($".{fieldAccess.Field.Name}");
                    }
                    
                    //FunctionExpression -> do nothing
                    //MethodAccessExpression -> do nothing
                }

                nameBuilder.Append($".{fieldName}");
                return nameBuilder.ToString();
            }
            else
                throw new Exception($"Unexpected bound expression kind {left.Kind}");
        }

        private void EmitLiteralAssignment(string varName, BoundLiteralExpression literal, FunctionEmittion emittion)
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
                emittion.AppendLine(command);
            }
            else if (literal.Type == TypeSymbol.Float)
            {
                var value = (float)literal.Value;
                var command = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Float)} \"{varName}\" set value {value}f";
                emittion.AppendLine(command);
            }
            else if (literal.Type == TypeSymbol.Double)
            {
                var value = (double)literal.Value;
                var command = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Double)} \"{varName}\" set value {value}d";
                emittion.AppendLine(command);
            }
            else
            {
                int value;

                if (literal.Type == TypeSymbol.Int)
                    value = (int)literal.Value;
                else
                    value = ((bool)literal.Value) ? 1 : 0;

                var command = $"scoreboard players set {varName} {Vars} {value}";
                emittion.AppendLine(command);
            }
        }

        private void EmitVariableAssignment(string varName, VariableSymbol otherVar, FunctionEmittion emittion)
        {
            if (otherVar is StringEnumMemberSymbol stringEnumMember)
            {
                var storage = _nameTranslator.GetStorage(otherVar.Type);
                var value = stringEnumMember.UnderlyingValue;
                var command = $"data modify storage {storage} \"{varName}\" set value \"{value}\"";
                emittion.AppendLine(command);
            }
            else if (otherVar is IntEnumMemberSymbol intEnumMember)
            {
                var value = intEnumMember.UnderlyingValue;
                var command = $"scoreboard players set {varName} {Vars} {value}";
                emittion.AppendLine(command);
            }
            else 
                EmitVariableAssignment(varName, _nameTranslator.GetVariableName(otherVar), otherVar.Type, emittion);
        }

        private void EmitVariableAssignment(string varName, string otherName, TypeSymbol type, FunctionEmittion emittion)
        {
            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string, float, double literal    -> data modify storage strings *this set from storage strings *other
            //named type        -> assign all the fields to the corresponding ones of the object we are copying

            if (varName == otherName)
                return;

            if (IsStorageType(type))
            {
                var storage = _nameTranslator.GetStorage(type);
                var command = $"data modify storage {storage} \"{varName}\" set from storage {storage} \"{otherName}\"";
                emittion.AppendLine(command);
            }
            else if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol intI)
            {
                var command = $"scoreboard players operation {varName} {Vars} = {otherName} {Vars}";
                emittion.AppendLine(command);
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

        private void EmitObjectCreationAssignment(string varName, BoundObjectCreationExpression objectCreationExpression, FunctionEmittion emittion, int current)
        {
            //Reserve a name for an object
            //Execute the constructor with the arguments

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>
            
            emittion.AppendComment($"Emitting object creation of type {objectCreationExpression.NamedType.Name}, stored in reference variable {varName}");

            var constructor = objectCreationExpression.NamedType.Constructor;
            var setParameters = EmitFunctionParametersAssignment(constructor.Parameters, objectCreationExpression.Arguments, emittion);

            Debug.Assert(constructor.FunctionBody != null);

            //We do this so that the constructor block knows the "this" instance name
            var currentContextName = _contextName;
            _contextName = varName;
            EmitBlockStatement(constructor.FunctionBody, emittion);
            _contextName = currentContextName;

            EmitFunctionParameterCleanUp(setParameters, emittion);
        }

        private void EmitUnaryExpressionAssignment(string name, BoundUnaryExpression unary, FunctionEmittion emittion, int current)
        {
            //TODO: Add constant assignment to load function

            //Identity -> Assign the expression normally
            //Negation -> Assign the expression normally, than multiply it by -1
            //Logical negation
            //         -> Assign the expression to <.temp> variable
            //            If it is 1, set the <*name> to 0
            //            If it is 0, set the <*name> to 1

            emittion.AppendLine();
            emittion.AppendComment($"Emitting unary expression \"{unary}\" to \"{name}\"");
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
                        emittion.AppendLine(command);
                    }
                    else
                    {
                        var storage = _nameTranslator.GetStorage(operand.Type);
                        var stringStorage = _nameTranslator.GetStorage(TypeSymbol.String);

                        emittion.AppendLine($"data modify storage {stringStorage} **macros.a set from storage {storage} {varName}");
                        emittion.AppendLine($"data modify storage {stringStorage} \"**sign\" set string storage {storage} {varName} 0 1");
                        emittion.AppendLine($"data modify storage {stringStorage} \"**last\" set string storage {storage} {varName} -1");
                        emittion.AppendLine($"execute if data storage {stringStorage} {{ \"**sign\": \"-\"}} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 1");

                        var typeSuffix = operand.Type == TypeSymbol.Float ? "f" : "d";
                        emittion.AppendLine($"execute if data storage {stringStorage} {{ \"**last\": \"{typeSuffix}\"}} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 0 -1");

                        emittion.AppendLine($"execute if data storage {stringStorage} {{ \"**sign\": \"-\"}} run data modify storage {stringStorage} **macros.sign set value \"\"");
                        emittion.AppendLine($"execute unless data storage {stringStorage} {{ \"**sign\": \"-\"}} run data modify storage {stringStorage} **macros.sign set value \"-\"");

                        FunctionEmittion macro;

                        if (operand.Type == TypeSymbol.Float)
                        {
                            macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateFloat, out bool isCreated);
                            if (isCreated)
                                macro.AppendMacro($"data modify storage {stringStorage} **macros.return set value $(sign)$(a)f");
                        }
                        else
                        {
                            macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.NegateDouble, out bool isCreated);
                            if (isCreated)
                                macro.AppendMacro($"data modify storage {stringStorage} **macros.return set value $(sign)$(a)d");
                        }

                        emittion.AppendLine($"function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} **macros");
                        emittion.AppendLine($"data modify storage {storage} {name} set from storage {stringStorage} **macros.return");

                        EmitCleanUp("**sign", TypeSymbol.String, emittion);
                        EmitCleanUp("**last", TypeSymbol.String, emittion);
                        EmitMacroCleanUp(emittion);
                    }
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:

                    var tempName = EmitAssignmentToTemp(operand, emittion, current);
                    var command1 = $"execute if score {tempName} {Vars} matches 1 run scoreboard players set {name} {Vars} 0";
                    var command2 = $"execute if score {tempName} {Vars} matches 0 run scoreboard players set {name} {Vars} 1";
                    emittion.AppendLine(command1);
                    emittion.AppendLine(command2);
                    EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
                    break;
                default:
                    throw new Exception($"Unexpected unary operator kind {operatorKind}");
            }
        }

        private void EmitBinaryExpressionAssignment(string name, BoundBinaryExpression binary, FunctionEmittion emittion, int current)
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

            emittion.AppendLine();
            emittion.AppendComment($"Emitting binary expression \"{binary}\" to \"{name}\"");
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
                        emittion.AppendLine("#String concatination is currently unsupported");
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
                        emittion.AppendLine(command1);
                        emittion.AppendLine(command2);
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
                        emittion.AppendLine(command1);
                        emittion.AppendLine(command2);
                        emittion.AppendLine(command3);
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
                            emittion.AppendLine(command1);
                            emittion.AppendLine(command2);
                            emittion.AppendLine(command3);
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
                            emittion.AppendLine(command1);
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

        private void EmitIntComparisonOperation(FunctionEmittion emittion, BoundExpression left, BoundExpression right, string name, BoundBinaryOperatorKind operation, int index)
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
                    emittion.AppendLine($"scoreboard players set {name} {Vars} {result}");
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
                        emittion.AppendLine(command1);
                        emittion.AppendLine(command2);
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

            emittion.AppendLine(command1);
            emittion.AppendLine(command2);
        }

        private void EmitFloatingPointComparisonOperation(FunctionEmittion emittion, string name, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operatorKind, int current)
        {
            var stringStorage = _nameTranslator.GetStorage(TypeSymbol.String);
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.PositionY, out bool isCreated);
            if (isCreated)
                macro.AppendMacro($"tp @s {DEBUG_CHUNK_X} $(a) {DEBUG_CHUNK_Z}");

            var temp = EmitAssignmentToTemp(TEMP, left, emittion, current + 1);
            emittion.AppendLine($"data modify storage {stringStorage} **macros.a set from storage {_nameTranslator.GetStorage(left.Type)} \"{temp}\"");
            emittion.AppendLine($"execute as {_nameTranslator.MathEntity1} run function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} **macros");

            temp = EmitAssignmentToTemp(TEMP, right, emittion, current + 1);
            emittion.AppendLine($"data modify storage {stringStorage} **macros.a set from storage {_nameTranslator.GetStorage(right.Type)} \"{temp}\"");
            emittion.AppendLine($"execute as {_nameTranslator.MathEntity2} run function {_nameTranslator.GetCallLink(macro)} with storage {stringStorage} **macros");
            
            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Equals:
                    {
                        emittion.AppendLine($"scoreboard players set {name} {Vars} 0");
                        emittion.AppendLine($"execute as {_nameTranslator.MathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {name} {Vars} 1");
                        break;
                    }
                case BoundBinaryOperatorKind.NotEquals:
                    {
                        emittion.AppendLine($"scoreboard players set {name} {Vars} 1");
                        emittion.AppendLine($"execute as {_nameTranslator.MathEntity1} at @s if entity @e[type=item_display,tag=!first,tag=blz,tag=debug,distance=..0.0001] run scoreboard players set {name} {Vars} 0");
                        break;
                    }
                case BoundBinaryOperatorKind.Greater:
                    {
                        emittion.AppendLine($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AppendLine($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".greater");
                        emittion.AppendLine($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {name} {Vars} 0");
                        emittion.AppendLine($"tag @e[tag=.this] remove .this");
                        break;
                    }
                case BoundBinaryOperatorKind.Less:
                    {
                        emittion.AppendLine($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AppendLine($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".less");
                        emittion.AppendLine($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {name} {Vars} 0");
                        emittion.AppendLine($"tag @e[tag=.this] remove .this");
                        break;
                    }
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    {
                        emittion.AppendLine($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AppendLine($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".greater");
                        emittion.AppendLine($"tag @e[tag=.this] remove .this");
                        break;
                    }
                case BoundBinaryOperatorKind.LessOrEquals:
                    {
                        emittion.AppendLine($"execute positioned {DEBUG_CHUNK_X} 19999999.9999 {DEBUG_CHUNK_Z} run tag @e[type=item_display,tag=blz,tag=debug,sort=nearest,limit=1] add .this");
                        emittion.AppendLine($"execute store result score {name} {Vars} run data get entity @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] item.components.\"minecraft:custom_data\".less");
                        emittion.AppendLine($"execute at @e[type=item_display,tag=blz,tag=debug,tag=.this,limit=1] if entity @e[type=item_display,tag=blz,tag=debug,tag=!.this,distance=..0.0001] run scoreboard players set {name} {Vars} 1");
                        emittion.AppendLine($"tag @e[tag=.this] remove .this");
                        break;
                    }
            }

            EmitCleanUp(temp, left.Type, emittion);
            if (left.Type != right.Type)
                EmitCleanUp(temp, right.Type, emittion);

            EmitMacroCleanUp(emittion);
        }

        private void EmitIntBinaryOperation(string name, FunctionEmittion emittion, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int current)
        {
            var leftName = EmitAssignmentExpression(name, left, emittion, current);
            var rightName = string.Empty;

            if (right is BoundLiteralExpression l)
            {
                if (operation == BoundBinaryOperatorKind.Addition)
                {
                    var command1 = $"scoreboard players add {leftName} {Vars} {l.Value}";
                    emittion.AppendLine(command1);
                    return;
                }
                else if (operation == BoundBinaryOperatorKind.Subtraction)
                {
                    var command1 = $"scoreboard players remove {leftName} {Vars} {l.Value}";
                    emittion.AppendLine(command1);
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
            emittion.AppendLine(command);
        }

        private void EmitFloatingPointBinaryOperation(string name, FunctionEmittion emittion, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind kind, int current)
        {
            var leftName = EmitAssignmentExpression(name, left, emittion, current);
            var rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1);
            var storage = _nameTranslator.GetStorage(left.Type);

            if (kind != BoundBinaryOperatorKind.Subtraction)
            {
                emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a set from storage {storage} \"{leftName}\"");
                emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.b set from storage {_nameTranslator.GetStorage(right.Type)} \"{rightName}\"");
            }

            FunctionEmittion macro;
            var entity = _nameTranslator.MathEntity1.ToString();

            switch (kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Add, out bool isCreated);
                        if (isCreated)
                            macro.AppendMacro($"execute positioned ~ $(a) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(b) {DEBUG_CHUNK_Z}");
                        break;
                    }
                case BoundBinaryOperatorKind.Subtraction:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Subtract, out bool isCreated);
                        var stringStorage = _nameTranslator.GetStorage(TypeSymbol.String);
                        var sub = macro.Children.FirstOrDefault(n => n.Name == "if_minus");

                        if (sub == null)
                        {
                            sub = FunctionEmittion.CreateSub(macro, "if_minus");
                            sub.AppendLine($"data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 1");
                            sub.AppendLine($"data modify storage {stringStorage} \"**last\" set string storage {stringStorage} **macros.a -1");
                            sub.AppendLine($"execute if data storage {stringStorage} {{ \"**last\" : \"d\" }} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 0 -1");
                            sub.AppendLine($"execute if data storage {stringStorage} {{ \"**last\" : \"f\" }} run data modify storage {stringStorage} **macros.a set string storage {stringStorage} **macros.a 0 -1");
                            sub.AppendLine($"data modify storage {stringStorage} **macros.polarity set value \"\"");
                        }

                        if (isCreated)
                            macro.AppendMacro($"execute positioned ~ $(b) ~ run tp {entity} {DEBUG_CHUNK_X} ~$(polarity)$(a) {DEBUG_CHUNK_Z}");

                        emittion.AppendLine($"data modify storage {stringStorage} **macros.b set from storage {storage} \"{leftName}\"");
                        emittion.AppendLine($"data modify storage {stringStorage} **macros.a set from storage {_nameTranslator.GetStorage(right.Type)} \"{rightName}\"");
                        emittion.AppendLine($"data modify storage {stringStorage} \"**pol\" set from storage {_nameTranslator.GetStorage(right.Type)} \"{rightName}\"");
                        emittion.AppendLine($"data modify storage {stringStorage} \"**pol\" set string storage {stringStorage} \"**pol\" 0 1");

                        emittion.AppendLine($"execute if data storage {stringStorage} {{ \"**pol\" : \"-\" }} run function {_nameTranslator.GetCallLink(sub)}");
                        emittion.AppendLine($"execute unless data storage {stringStorage} {{ \"**pol\" : \"-\" }} run data modify storage {stringStorage} **macros.polarity set value \"-\"");
            
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
                            macro.AppendMacro($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} {RETURN_TEMP_NAME} set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f]");
                            macro.AppendMacro($"data modify entity {entity} transformation set value [0f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,$(b)f]");
                        }
                        break;
                    }
                case BoundBinaryOperatorKind.Division:
                    {
                        macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.Divide, out bool isCreated);

                        if (isCreated)
                            macro.AppendMacro($"data modify entity {entity} transformation set value [0f, 0f, 0f,$(a)f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,$(b)f]");
                        break;
                    }

                default:
                    throw new Exception($"Unexpected binary operation kind {kind}");
            }

            emittion.AppendLine($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");

            if (kind == BoundBinaryOperatorKind.Addition || kind == BoundBinaryOperatorKind.Subtraction)
            {
                emittion.AppendLine($"data modify storage {storage} {name} set from entity {_nameTranslator.MathEntity1.ToString()} Pos[1]");
                emittion.AppendLine($"tp {entity} {DEBUG_CHUNK_X} 0 {DEBUG_CHUNK_Z}");

                if (left.Type == TypeSymbol.Float)
                    EmitFloatConversion(name, name, TypeSymbol.Float, emittion);

            }
            else if (kind == BoundBinaryOperatorKind.Multiplication)
            {
                emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} {RETURN_TEMP_NAME}[-1] set from entity {entity} transformation.translation[0]");
                emittion.AppendLine($"data modify entity {entity} transformation set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} {RETURN_TEMP_NAME}");
                emittion.AppendLine($"data modify storage {storage} {name} set from entity {entity} transformation.translation[0]");
                EmitCleanUp(RETURN_TEMP_NAME, TypeSymbol.String, emittion);

                if (left.Type == TypeSymbol.Double)
                    EmitDoubleConversion(name, name, TypeSymbol.Double, emittion);
            }
            else if (kind == BoundBinaryOperatorKind.Division)
            {
                emittion.AppendLine($"data modify storage {storage} {name} set from entity {entity} transformation.translation[0]");

                if (left.Type == TypeSymbol.Double)
                    EmitDoubleConversion(name, name, TypeSymbol.Double, emittion);
            }
            
            EmitMacroCleanUp(emittion);
        }
        

        private void EmitCallExpressionAssignment(string name, BoundCallExpression call, FunctionEmittion emittion, int current)
        {
            //The return value via a temp variable also works for int and bool, but since
            //Mojang's added /return why not use it instead

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

            emittion.AppendComment($"Assigning return value of {call.Function.Name} to \"{name}\"");

            if (call.Function.ReturnType == TypeSymbol.Int || call.Function.ReturnType == TypeSymbol.Bool || call.Function.ReturnType is EnumSymbol)
            {
                var isBuiltIt = TryEmitBuiltInFunction(name, call, emittion, current);

                if (!isBuiltIt)
                {
                    var setParameters = EmitFunctionParametersAssignment(call.Function.Parameters, call.Arguments, emittion);
                    var command = $"execute store result score {name} {Vars} run function {_nameTranslator.GetCallLink(call.Function)}";
                    emittion.AppendLine(command);
                    EmitFunctionParameterCleanUp(setParameters, emittion);
                }
            }
            else
            {
                EmitCallExpression(name, call, emittion, current);
                var command2 = $"data modify storage {_nameTranslator.GetStorage(call.Type)} \"{name}\" set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{RETURN_TEMP_NAME}\"";
                emittion.AppendLine(command2);
            }
        }

        private Dictionary<ParameterSymbol, string> EmitFunctionParametersAssignment(ImmutableArray<ParameterSymbol> parameters, ImmutableArray<BoundExpression> arguments, FunctionEmittion emittion)
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

        private void EmitFunctionParameterCleanUp(Dictionary<ParameterSymbol, string> parameters, FunctionEmittion emittion)
        {
            foreach (var parameter in parameters.Keys)
            {
                var name = parameters[parameter];
                EmitCleanUp(name, parameter.Type, emittion);
            }
        }

        private void EmitConversionExpressionAssignment(string name, BoundConversionExpression conversion, FunctionEmittion emittion, int current)
        {
            //to int -> scoreboard players operation
            //to string -> copy to storage, than copy with data modify ... string
            //to object -> data modify storage

            emittion.AppendComment($"Assigning a conversion from {conversion.Expression.Type} to {conversion.Type} to variable \"{name}\"");
            var resultType = conversion.Type;
            var sourceType = conversion.Expression.Type;
            var tempName = EmitAssignmentToTemp(conversion.Expression, emittion, current);

            if (resultType == TypeSymbol.String)
            {
                var stringsStorage = _nameTranslator.GetStorage(TypeSymbol.String);
                var tempPath = "TEMP.*temp1";

                if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                {
                    var command1 = $"execute store result storage {stringsStorage} \"{tempPath}\" int 1 run scoreboard players get {tempName} {Vars}";
                    var command2 = $"data modify storage {stringsStorage} \"{name}\" set string storage {stringsStorage} \"{tempPath}\"";
                    emittion.AppendLine(command1);
                    emittion.AppendLine(command2);
                    EmitCleanUp(tempPath, resultType, emittion);
                }
                else if (sourceType is EnumSymbol enumSymbol)
                {
                    if (enumSymbol.IsIntEnum)
                    {
                        foreach (var enumMember in enumSymbol.Members)
                        {
                            var intMember = (IntEnumMemberSymbol) enumMember;
                            var command = $"execute if score {tempName} {Vars} matches {intMember.UnderlyingValue} run data modify storage {stringsStorage} \"{name}\" set value \"{enumMember.Name}\"";
                            emittion.AppendLine(command);
                        }
                    }
                    else
                    {
                        var enumStorage = _nameTranslator.GetStorage(enumSymbol);
                        var command = $"data modify storage {stringsStorage} \"{name}\" set from storage {enumStorage} \"{tempName}\"";
                        emittion.AppendLine(command);
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
                    emittion.AppendLine($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.Object)} \"{name}\" int 1 run scoreboard players get {tempName} {Vars}");
                }
                else
                {
                    emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Object)} \"{name}\" set from storage {_nameTranslator.GetStorage(TypeSymbol.String)} \"{tempName}\"");
                }
            }
            EmitCleanUp(tempName, sourceType, emittion);
        }

        private void EmitFloatConversion(string name, string otherName, TypeSymbol sourceType, FunctionEmittion emittion)
        {
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToFloat, out bool isCreated);
            var storage = _nameTranslator.GetStorage(TypeSymbol.Float);

            if (isCreated)
                macro.AppendMacro($"data modify storage {storage} {RETURN_TEMP_NAME} set value $(a)f");

            if (sourceType == TypeSymbol.Int)
                emittion.AppendLine($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a int 1 run scoreboard players get {otherName} {Vars}");
            else
                emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a set from storage {_nameTranslator.GetStorage(sourceType)} \"{otherName}\"");

            emittion.AppendLine($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");
            emittion.AppendLine($"data modify storage {storage} {name} set from storage {storage} {RETURN_TEMP_NAME}");
            EmitMacroCleanUp(emittion);
        }

        private void EmitDoubleConversion(string name, string sourceName, TypeSymbol sourceType, FunctionEmittion emittion, string? emittionStorage = null)
        {
            var macro = GetOrCreateBuiltIn(BuiltInNamespace.Blaze.Math.ToDouble, out bool isCreated);
            var storage = _nameTranslator.GetStorage(TypeSymbol.Double);

            if (isCreated)
                macro.AppendMacro($"data modify storage {storage} {RETURN_TEMP_NAME} set value $(a)d");

            if (sourceType == TypeSymbol.Int)
                emittion.AppendLine($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a int 1 run scoreboard players get {sourceName} {Vars}");
            else
                emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.a set from storage {_nameTranslator.GetStorage(sourceType)} \"{sourceName}\"");

            var resultStorage = emittionStorage ?? storage;
            emittion.AppendLine($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");
            emittion.AppendLine($"data modify storage {resultStorage} {name} set from storage {storage} {RETURN_TEMP_NAME}");
            EmitMacroCleanUp(emittion);
        }

        private void EmitCleanUp(string name, TypeSymbol type, FunctionEmittion emittion)
        {
            string command;

            if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol e && e.IsIntEnum)
                command = $"scoreboard players reset {name} {Vars}";
            else
                command = $"data remove storage {_nameTranslator.GetStorage(type)} \"{name}\"";

            emittion.AppendCleanUp(command);
        }

        private void EmitMacroCleanUp(FunctionEmittion emittion) => EmitCleanUp("**macros", TypeSymbol.String, emittion);

        private string EmitAssignmentToTemp(string tempName, BoundExpression expression, FunctionEmittion emittion, int index, bool addDot = true)
        {
            var varName = $"{(addDot ? "." : string.Empty)}{tempName}{index}";
            var resultName = EmitAssignmentExpression(varName, expression, emittion, index);
            return resultName;
        }

        private string EmitAssignmentToTemp(BoundExpression expression, FunctionEmittion emittion, int index) => EmitAssignmentToTemp(TEMP, expression, emittion, index);

        private bool IsStorageType(TypeSymbol type)
        {
            return type == TypeSymbol.String ||
                   type == TypeSymbol.Object ||
                   type == TypeSymbol.Float ||
                   type == TypeSymbol.Double ||
                   type is EnumSymbol e && !e.IsIntEnum;
        }
    }
}