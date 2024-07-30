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

        private string? _contextName = null;
        private string RootNamespace => _configuration.RootNamespace;
        private string TEMP => EmittionNameTranslator.TEMP;
        private string RETURN_TEMP_NAME => EmittionNameTranslator.RETURN_TEMP_NAME;

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
            _initFunction.AppendLine("scoreboard objectives add vars dummy");
            _initFunction.AppendLine("scoreboard objectives add CONST dummy");
            _initFunction.AppendLine("scoreboard players set *-1 CONST -1");
            _initFunction.AppendLine();
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

            var datapack = new Datapack(_configuration, functionNamespaceEmittionBuilder.ToImmutable(), _initFunction, _tickFunction);
            datapack.Build();
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
            var callClauseCommand = $"execute if score {tempName} vars matches 1 run function {_nameTranslator.GetCallLink(subFunction)}";

            emittion.AppendLine(callClauseCommand);

            if (node.ElseBody != null)
            {
                var elseSubFunction = FunctionEmittion.CreateSub(emittion, SubFunctionKind.Else);
                EmitStatement(node.ElseBody, elseSubFunction);
                var elseCallClauseCommand = $"execute if score {tempName} vars matches 0 run function {_nameTranslator.GetCallLink(elseSubFunction)}";

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
            var breakClauseCommand = $"execute if score {tempName} vars matches 0 run return 0";
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
            var loopClauseCommand = $"execute if score {tempName} vars matches 1 run {callCommand}";
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

            if (returnExpression.Type == TypeSymbol.Int || returnExpression.Type == TypeSymbol.Bool)
            {
                var returnCommand = $"return run scoreboard players get {returnName} vars";
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
                EmitCallExpression(call, emittion);
            }
            else
            {
                throw new Exception($"Unexpected expression statement kind {expression.Kind}");
            }
        }

        private void EmitCallExpression(BoundCallExpression call, FunctionEmittion emittion)
        {
            //Can be a built-in function -> TryEmitBuiltInFunction();
            //Can be a user defined function ->
            //
            //Assign every parameter to temp variable
            //function <function>
            //Reset every parameter

            var isBuiltIt = TryEmitBuiltInFunction(call, emittion);
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
                EmitCallExpressionAssignment(name, c, emittion);
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
                var otherName = GetNameOfAssignableExpression(fieldExpression);
                EmitVariableAssignment(name, otherName, right.Type, emittion);
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
            //string literal    -> data modify storage strings string <SOURCE> [<sourcePath>]
            //bool literal      -> scoreboard players set *v bools <value>

            if (literal.Type == TypeSymbol.String)
            {
                var value = (string)literal.Value;
                var command = $"data modify storage strings {varName} set value \"{value}\"";
                emittion.AppendLine(command);
            }
            else
            {
                int value;

                if (literal.Type == TypeSymbol.Int)
                    value = (int)literal.Value;
                else
                    value = ((bool)literal.Value) ? 1 : 0;

                var command = $"scoreboard players set {varName} vars {value}";
                emittion.AppendLine(command);
            }
        }

        private void EmitVariableAssignment(string varName, VariableSymbol otherVar, FunctionEmittion emittion)
            => EmitVariableAssignment(varName, _nameTranslator.GetVariableName(otherVar), otherVar.Type, emittion);

        private void EmitVariableAssignment(string varName, string otherName, TypeSymbol type, FunctionEmittion emittion)
        {
            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string literal    -> data modify storage strings *this set from storage strings *other
            //named type        -> assign all the fields to the corresponding ones of the object we are copying

            if (varName == otherName)
                return;

            if (type == TypeSymbol.String || type == TypeSymbol.Object)
            {
                var storage = _nameTranslator.GetStorage(type);
                var command = $"data modify storage {storage} {varName} set from storage {storage} {otherName}";
                emittion.AppendLine(command);
            }
            else if (type == TypeSymbol.Int || type == TypeSymbol.Bool)
            {
                var command = $"scoreboard players operation {varName} vars = {otherName} vars";
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

            emittion.AppendComment($"Emitting unary expression \"{unary}\" to \"{name}\"");
            var expression = unary.Operand;
            var operatorKind = unary.Operator.OperatorKind;

            switch (operatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    EmitAssignmentExpression(name, expression, emittion, current);
                    break;
                case BoundUnaryOperatorKind.Negation:

                    var varName = EmitAssignmentExpression(name, expression, emittion, current);
                    var command = $"scoreboard players operation {varName} vars *= *-1 CONST";
                    emittion.AppendLine(command);
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:

                    var tempName = EmitAssignmentToTemp(expression, emittion, current);
                    var command1 = $"execute if score {tempName} vars matches 1 run scoreboard players set {name} vars 0";
                    var command2 = $"execute if score {tempName} vars matches 0 run scoreboard players set {name} vars 1";
                    emittion.AppendLine(command1);
                    emittion.AppendLine(command2);
                    EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
                    break;
                default:
                    throw new Exception($"Unexpected unary operator kind {operatorKind}");
            }
            emittion.AppendLine();
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
                    else
                    {
                        emittion.AppendLine("#String concatination is currently unsupported");
                    }
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                case BoundBinaryOperatorKind.Multiplication:
                case BoundBinaryOperatorKind.Division:
                    EmitIntBinaryOperation(name, emittion, left, right, operatorKind, current);
                    break;
                case BoundBinaryOperatorKind.LogicalMultiplication:
                    {
                        var leftName = EmitAssignmentToTemp($"lbTemp", left, emittion, current + 1);
                        var rightName = EmitAssignmentToTemp($"rbTemp", right, emittion, current + 1);

                        var command1 = $"scoreboard players set {name} vars 0";
                        var command2 = $"execute if score {leftName} vars matches 1 if score {rightName} vars matches 1 run scoreboard players set {name} vars 1";
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

                        var command1 = $"scoreboard players set {name} vars 0";
                        var command2 = $"execute if score {leftName} vars matches 1 run scoreboard players set {name} vars 1";
                        var command3 = $"execute if score {rightName} vars matches 1 run scoreboard players set {name} vars 1";
                        emittion.AppendLine(command1);
                        emittion.AppendLine(command2);
                        emittion.AppendLine(command3);
                        EmitCleanUp(leftName, left.Type, emittion);
                        EmitCleanUp(rightName, right.Type, emittion);
                    }
                    break;
                case BoundBinaryOperatorKind.Equals:
                    if (left.Type == TypeSymbol.String || left.Type == TypeSymbol.Object)
                    {
                        var leftName = EmitAssignmentToTemp("lTemp", left, emittion, current + 1, false);
                        var rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1, false);

                        var command1 = $"execute store success score {TEMP} vars run data modify storage strings {leftName} set from storage strings {rightName}";
                        var command2 = $"execute if score {TEMP} vars matches 1 run scoreboard players set {name} vars 0";
                        var command3 = $"execute if score {TEMP} vars matches 0 run scoreboard players set {name} vars 1";
                        emittion.AppendLine(command1);
                        emittion.AppendLine(command2);
                        emittion.AppendLine(command3);
                        EmitCleanUp(leftName, left.Type, emittion);
                        EmitCleanUp(rightName, right.Type, emittion);
                        EmitCleanUp(TEMP, TypeSymbol.Bool, emittion);
                    }
                    else
                    {
                        EmitComparisonBinaryOperation(emittion, left, right, name, operatorKind, current);
                    }
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    if (left.Type == TypeSymbol.String)
                    {
                        var leftName = EmitAssignmentToTemp("lTemp", left, emittion, current + 1, false);
                        var rightName = EmitAssignmentToTemp("rTemp", right, emittion, current + 1, false);

                        var command1 = $"execute store success score {name} vars run data modify storage strings {leftName} set from storage strings {rightName}";
                        emittion.AppendLine(command1);
                        EmitCleanUp(leftName, left.Type, emittion);
                        EmitCleanUp(rightName, right.Type, emittion);
                    }
                    else
                    {
                        EmitComparisonBinaryOperation(emittion, left, right, name, operatorKind, current);
                    }
                    break;
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.LessOrEquals:
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    EmitComparisonBinaryOperation(emittion, left, right, name, operatorKind, current);
                    break;
            }
            emittion.AppendLine();
        }

        private void EmitComparisonBinaryOperation(FunctionEmittion emittion, BoundExpression left, BoundExpression right, string name, BoundBinaryOperatorKind operation, int index)
        {
            var leftName = string.Empty;
            if (left is BoundVariableExpression v)
            {
                leftName = _nameTranslator.GetVariableName(v.Variable);
            }
            else
            {
                leftName = EmitAssignmentToTemp("lTemp", left, emittion, index + 1);
                EmitCleanUp(leftName, left.Type, emittion);
            }
                
            var initialValue = operation == BoundBinaryOperatorKind.NotEquals ? 1 : 0;
            var successValue = operation == BoundBinaryOperatorKind.NotEquals ? 0 : 1;

            var command1 = $"scoreboard players set {name} vars {initialValue}";
            var command2 = string.Empty;
            if (right is BoundLiteralExpression l && l.Value is int)
            {
                int value = (int) l.Value;
                var comparason = "matches" + operation switch
                {
                    BoundBinaryOperatorKind.Less => ".." + (value - 1).ToString(),
                    BoundBinaryOperatorKind.LessOrEquals => ".." + value,
                    BoundBinaryOperatorKind.Greater => (value + 1).ToString() + "..",
                    BoundBinaryOperatorKind.GreaterOrEquals => value + "..",
                    _ => value
                };
                command2 = $"execute unless score {leftName} vars {comparason} run scoreboard players set {name} vars {successValue}";
            }
            else
            {
                var rightName = string.Empty;
                if (right is BoundVariableExpression vr)
                {
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
                command2 = $"execute if score {leftName} vars {operationSign} {rightName} vars run scoreboard players set {name} vars {successValue}";
            }

            emittion.AppendLine(command1);
            emittion.AppendLine(command2);
        }

        private void EmitIntBinaryOperation(string name, FunctionEmittion emittion, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int index)
        {
            var leftName = EmitAssignmentExpression(name, left, emittion, index);
            var rightName = string.Empty;

            if (right is BoundLiteralExpression l)
            {
                if (operation == BoundBinaryOperatorKind.Addition)
                {
                    var command1 = $"scoreboard players add {leftName} vars {l.Value}";
                    emittion.AppendLine(command1);
                    return;
                }
                else if (operation == BoundBinaryOperatorKind.Subtraction)
                {
                    var command1 = $"scoreboard players remove {leftName} vars {l.Value}";
                    emittion.AppendLine(command1);
                    return;
                }
            }
            else if (right is BoundVariableExpression v)
            {
                rightName = _nameTranslator.GetVariableName(v.Variable);
            }
            else
            {
                rightName = EmitAssignmentToTemp("rTemp", right, emittion, index + 1);
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
            var command = $"scoreboard players operation {leftName} vars {operationSign} {rightName} vars";
            emittion.AppendLine(command);
        }

        private void EmitCallExpressionAssignment(string name, BoundCallExpression call, FunctionEmittion emittion)
        {
            //The return value via a temp variable also works for int and bool, but since
            //Mojang's added /return why not use it instead

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

            emittion.AppendComment($"Assigning return value of {call.Function.Name} to \"{name}\"");

            if (call.Function.ReturnType == TypeSymbol.Int || call.Function.ReturnType == TypeSymbol.Bool)
            {
                var setParameters = EmitFunctionParametersAssignment(call.Function.Parameters, call.Arguments, emittion);
                var command = $"execute store result score {name} vars run function {_nameTranslator.GetCallLink(call.Function)}";
                emittion.AppendLine(command);

                EmitFunctionParameterCleanUp(setParameters, emittion);
            }
            else
            {
                EmitCallExpression(call, emittion);
                var command2 = $"data modify storage {_nameTranslator.GetStorage(call.Type)} {name} set from storage strings {RETURN_TEMP_NAME}";
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

        private void EmitConversionExpressionAssignment(string name, BoundConversionExpression conv, FunctionEmittion emittion, int current)
        {
            //to int -> scoreboard players operation
            //to string -> copy to storage, than copy with data modify ... string
            //to object -> data modify storage

            emittion.AppendComment($"Assigning a conversion from {conv.Expression.Type} to {conv.Type} to variable \"{name}\"");
            var resultType = conv.Type;
            var sourceType = conv.Expression.Type;
            var tempName = EmitAssignmentToTemp(conv.Expression, emittion, current);

            if (resultType == TypeSymbol.String && (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool))
            {
                var tempPath = "TEMP.*temp1";
                var command1 = $"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} {tempPath} int 1 run scoreboard players get {tempName} vars";
                var command2 = $"data modify storage strings {name} set string storage strings {tempPath}";
                emittion.AppendLine(command1);
                emittion.AppendLine(command2);
                EmitCleanUp(tempName, sourceType, emittion);
                EmitCleanUp(tempPath, resultType, emittion);
            }
            if (resultType == TypeSymbol.Object)
            {
                if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                {
                    var command = $"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.Object)} {name} int 1 run scoreboard players get {tempName} vars";
                    emittion.AppendLine(command);
                    EmitCleanUp(tempName, sourceType, emittion);
                }
                else
                {
                    var command = $"data modify storage {_nameTranslator.GetStorage(TypeSymbol.Object)} {name} set from storage strings {tempName}";
                    emittion.AppendLine(command);
                    EmitCleanUp(tempName, sourceType, emittion);
                }
            }
        }

        private void EmitCleanUp(string name, TypeSymbol type, FunctionEmittion emittion)
        {
            string command;

            if (type == TypeSymbol.Int || type == TypeSymbol.Bool)
                command = $"scoreboard players reset {name} vars";
            else
                command = $"data remove storage {_nameTranslator.GetStorage(type)} {name}";

            emittion.AppendCleanUp(command);
        }

        private string EmitAssignmentToTemp(string tempName, BoundExpression expression, FunctionEmittion emittion, int index, bool addDot = true)
        {
            var varName = $"{(addDot ? "." : string.Empty)}{tempName}{index}";
            var temp = new LocalVariableSymbol(varName, expression.Type, null);
            var resultName = EmitAssignmentExpression(temp, expression, emittion, index);
            return resultName;
        }

        private string EmitAssignmentToTemp(BoundExpression expression, FunctionEmittion emittion, int index) => EmitAssignmentToTemp("temp", expression, emittion, index);
    }
}