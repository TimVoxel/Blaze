using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Emit
{
    internal class DatapackEmitter
    {
        private const string TEMP = ".temp";
        private const string RETURN_TEMP_NAME = "return.value";

        private readonly BoundProgram _program;
        private readonly CompilationConfiguration _configuration;

        public DatapackEmitter(BoundProgram program, CompilationConfiguration configuration)
        {
            _program = program;
            _configuration = configuration;
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
            var emittionsBuilder = ImmutableArray.CreateBuilder<FunctionEmittion>();
            foreach (var function in _program.Functions)
            {
                var functionEmittion = EmitFunction(function.Key, function.Value);
                emittionsBuilder.Add(functionEmittion);
            }

            var datapack = new Datapack(_configuration, emittionsBuilder.ToImmutable());
            datapack.Build();
        }

        private FunctionEmittion EmitFunction(FunctionSymbol function, BoundStatement bodyBlock)
        {
            var emittion = new FunctionEmittion(function.Name);
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
                    EvaluateBlockStatement((BoundBlockStatement)node, emittion);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement)node, emittion);
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

        private void EvaluateBlockStatement(BoundBlockStatement node, FunctionEmittion emittion)
        {
            foreach (BoundStatement statement in node.Statements)
                EmitStatement(statement, emittion);
        }

        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement node, FunctionEmittion emittion)
        {
            var assignment = new BoundAssignmentExpression(node.Variable, node.Initializer);
            EmitAssignmentExpression(assignment, emittion, 0);
        }

        private void EvaluateIfStatement(BoundIfStatement node, FunctionEmittion emittion)
        {
            //Emit condition into <.temp>
            //execute if <.temp> run subfunction
            //else generate a sub function and run it instead
            //if there is an else clause generate another sub with the else body

            var subName = emittion.GetFreeSubIfName();
            var subFunction = new FunctionEmittion(subName);
            EmitStatement(node.Body, subFunction);

            var tempName = EmitAssignmentToTemp(node.Condition, emittion, 0);
            var callClauseCommand = $"execute if score {tempName} vars matches 1 run function ns:{subName}";

            emittion.AppendLine(callClauseCommand);
            emittion.Children.Add(subFunction);

            if (node.ElseBody != null)
            {
                var elseName = emittion.GetFreeSubElseName();
                var elseSubFunction = new FunctionEmittion(elseName);
                EmitStatement(node.ElseBody, elseSubFunction);
                var elseCallClauseCommand = $"execute if score {tempName} vars matches 0 run function ns:{elseName}";

                emittion.AppendLine(elseCallClauseCommand);          
                emittion.Children.Add(elseSubFunction);
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

            var subName = emittion.GetFreeSubLoopName();
            var subFunction = new FunctionEmittion(subName);
            var callCommand = $"function ns:{subName}";

            var tempName = EmitAssignmentToTemp(node.Condition, subFunction, 0);
            var breakClauseCommand = $"execute if score {tempName} vars matches 0 run return 0";
            subFunction.AppendLine(breakClauseCommand);
            subFunction.AppendLine();

            EmitStatement(node.Body, subFunction);
            subFunction.AppendLine();
            subFunction.AppendLine(callCommand);
            
            emittion.AppendLine(callCommand);
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
            emittion.Children.Add(subFunction);
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

            var subName = emittion.GetFreeSubLoopName();
            var subFunction = new FunctionEmittion(subName);
            var callCommand = $"function ns:{subName}";

            EmitStatement(node.Body, subFunction);

            var tempName = EmitAssignmentToTemp(node.Condition, subFunction, 0);
            var loopClauseCommand = $"execute if score {tempName} vars matches 1 run {callCommand}";
            subFunction.AppendLine(loopClauseCommand);
            subFunction.AppendLine();

            emittion.AppendLine(callCommand);
            EmitCleanUp(tempName, TypeSymbol.Bool, emittion);
            emittion.Children.Add(subFunction);
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

            var returnName = EmitAssignmentToTemp(RETURN_TEMP_NAME, returnExpression, emittion, 0, false);
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

        private void EvaluateExpressionStatement(BoundExpressionStatement node, FunctionEmittion emittion)
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
            //Can be a built-in function -> EmitBuildInFunction();
            //Can be a user defined function ->
            //
            //Assign every parameter to temp variable
            //function <function>
            //Reset every parameter

            var isBuiltIt = BuiltInFunctionEmitter.TryEmitBuiltInFunction(call, emittion);
            if (!isBuiltIt)
            {
                var setNames = EmitFunctionParametersAssignment(call, emittion);
                var functionName = call.Function.Name;
                var command = $"function ns:{functionName}";
                emittion.AppendLine(command);

                EmitFunctionParameterCleanUp(setNames, emittion);
            }
        }

        private void EmitNopStatement(FunctionEmittion emittion)
        {
            emittion.AppendLine("tellraw @a {\"text\":\"Nop statement in program\", \"color\":\"red\"}");
        }

        private string EmitAssignmentExpression(BoundAssignmentExpression assignment, FunctionEmittion emittion, int current) 
            => EmitAssignmentExpression(assignment.Variable, assignment.Expression, emittion, current);

        private string EmitAssignmentExpression(VariableSymbol variable, BoundExpression expression, FunctionEmittion emittion, int current)
        {
            //TODO: Add vars scoreboard in a load function
            var name = $"*{variable.Name}";

            if (expression is BoundLiteralExpression l)
            {
                EmitLiteralAssignment(name, l, emittion);
            }
            else if (expression is BoundVariableExpression v)
            {
                EmitVariableAssignment(name, v.Variable, emittion);
            }
            else if (expression is BoundAssignmentExpression a)
            {
                EmitAssignmentExpression(a, emittion, current);
                EmitVariableAssignment(name, a.Variable, emittion);
            }
            else if (expression is BoundUnaryExpression u)
            {
                EmitUnaryExpressionAssignment(variable, u, emittion, current);
            }
            else if (expression is BoundBinaryExpression b)
            {
                EmitBinaryExpressionAssignment(variable, b, emittion, current);
            }
            else if (expression is BoundCallExpression c)
            {
                EmitCallExpressionAssignment(name, c, emittion);
            }
            else if (expression is BoundConversionExpression conv)
            {
                EmitConversionExpressionAssignment(name, conv, emittion, current);
            }
            else
            {
                emittion.AppendLine("#Unsupported expression type");
            }
            return name;
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
                var value = literal.Type == TypeSymbol.Int ? (int)literal.Value
                                    : ((bool)literal.Value ? 1 : 0);

                var command = $"scoreboard players set {varName} vars {value}";
                emittion.AppendLine(command);
            }
        }

        private void EmitVariableAssignment(string varName, VariableSymbol otherVar, FunctionEmittion emittion)
        {
            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string literal    -> data modify storage strings *this set from storage strings *other

            var other = $"*{otherVar.Name}";

            if (otherVar.Type == TypeSymbol.String || otherVar.Type == TypeSymbol.Object)
            {
                var storageName = otherVar.Type == TypeSymbol.String ? "strings" : "objects";
                var command = $"data modify storage {storageName} {varName} set from storage {storageName} {other}";
                emittion.AppendLine(command);
            }
            else
            {
                var command = $"scoreboard players operation {varName} vars = {other} vars";
                emittion.AppendLine(command);
            }
        }

        private void EmitUnaryExpressionAssignment(VariableSymbol variable, BoundUnaryExpression unary, FunctionEmittion emittion, int current)
        {
            //TODO: Add constant assignment to load function

            //Identity -> Assign the expression normally
            //Negation -> Assign the expression normally, than multiply it by -1
            //Logical negation
            //         -> Assign the expression to <.temp> variable
            //            If it is 1, set the <*name> to 0
            //            If it is 0, set the <*name> to 1

            emittion.AppendComment($"Emitting unary expression \"{unary}\" to \"{variable.Name}\"");
            var expression = unary.Operand;
            var operatorKind = unary.Operator.OperatorKind;
            var name = $"*{variable.Name}";

            switch (operatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    EmitAssignmentExpression(variable, expression, emittion, current);
                    break;
                case BoundUnaryOperatorKind.Negation:

                    var varName = EmitAssignmentExpression(variable, expression, emittion, current);
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

        private void EmitBinaryExpressionAssignment(VariableSymbol variable, BoundBinaryExpression binary, FunctionEmittion emittion, int current)
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

            emittion.AppendComment($"Emitting binary expression \"{binary}\" to \"{variable.Name}\"");
            var left = binary.Left;
            var right = binary.Right;
            var operatorKind = binary.Operator.OperatorKind;
            var name = $"*{variable.Name}";

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Addition:

                    if (left.Type == TypeSymbol.Int)
                    {
                        EmitIntBinaryOperation(variable, emittion, left, right, operatorKind, current);
                    }
                    else
                    {
                        emittion.AppendLine("#String concatination is currently unsupported");
                    }
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                case BoundBinaryOperatorKind.Multiplication:
                case BoundBinaryOperatorKind.Division:
                    EmitIntBinaryOperation(variable, emittion, left, right, operatorKind, current);
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
                        EmitComparisonBinaryOperation(emittion, left, right, name, "=", current);
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
                        EmitComparisonBinaryOperation(emittion, left, right, name, "=", current, true);
                    }
                    break;
                case BoundBinaryOperatorKind.Less:
                    EmitComparisonBinaryOperation(emittion, left, right, name, "<", current);
                    break;
                case BoundBinaryOperatorKind.LessOrEquals:
                    EmitComparisonBinaryOperation(emittion, left, right, name, "<=", current);
                    break;
                case BoundBinaryOperatorKind.Greater:
                    EmitComparisonBinaryOperation(emittion, left, right, name, ">", current);
                    break;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    EmitComparisonBinaryOperation(emittion, left, right, name, ">=", current);
                    break;
            }
            emittion.AppendLine();
        }

        private void EmitComparisonBinaryOperation(FunctionEmittion emittion, BoundExpression left, BoundExpression right, string name, string sign, int index, bool inverted = false)
        {
            var leftName = EmitAssignmentToTemp("lTemp", left, emittion, index + 1);
            var rightName = EmitAssignmentToTemp("rTemp", right, emittion, index + 1);

            var initialValue = inverted ? 1 : 0;
            var successValue = inverted ? 0 : 1;

            var command1 = $"scoreboard players set {name} vars {initialValue}";
            var command2 = $"execute if score {leftName} vars {sign} {rightName} vars run scoreboard players set {name} vars {successValue}";
            emittion.AppendLine(command1);
            emittion.AppendLine(command2);
            EmitCleanUp(leftName, left.Type, emittion);
            EmitCleanUp(rightName, right.Type, emittion);
        }

        private void EmitIntBinaryOperation(VariableSymbol variable, FunctionEmittion emittion, BoundExpression left, BoundExpression right, BoundBinaryOperatorKind operation, int index)
        {
            var leftName = EmitAssignmentExpression(variable, left, emittion, index);
            var rightName = EmitAssignmentToTemp("rTemp", right, emittion, index + 1);

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
            EmitCleanUp(rightName, left.Type, emittion);
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
                var setParameters = EmitFunctionParametersAssignment(call, emittion);
                var command = $"execute store result score {name} vars run function ns:{call.Function.Name}";
                emittion.AppendLine(command);

                EmitFunctionParameterCleanUp(setParameters, emittion);
            }
            else
            {
                EmitCallExpression(call, emittion);
                var command2 = $"data modify storage strings {name} set from storage strings {RETURN_TEMP_NAME}";
                emittion.AppendLine(command2);
            }
        }

        private Dictionary<ParameterSymbol, string> EmitFunctionParametersAssignment(BoundCallExpression call, FunctionEmittion emittion)
        {
            var setNames = new Dictionary<ParameterSymbol, string>();

            for (int i = 0; i < call.Arguments.Count(); i++)
            {
                var argument = call.Arguments[i];
                var parameter = call.Function.Parameters[i];
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
                var command1 = $"execute store result storage strings {tempPath} int 1 run scoreboard players get {tempName} vars";
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
                    var command = $"execute store result storage objects {name} int 1 run scoreboard players get {tempName} vars";
                    emittion.AppendLine(command);
                    EmitCleanUp(tempName, sourceType, emittion);
                }
                else
                {
                    var command = $"data modify storage objects {name} set from storage strings {tempName}";
                    emittion.AppendLine(command);
                    EmitCleanUp(tempName, sourceType, emittion);
                }
            }
        }

        private void EmitCleanUp(string name, TypeSymbol type, FunctionEmittion emittion)
        {
            var command = $"scoreboard players reset {name} vars";

            if (type == TypeSymbol.Object)
            {
                command = $"data remove storage objects {name}";
            }
            else if (type == TypeSymbol.String)
            {
                command = $"data remove storage strings {name}";
            }
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