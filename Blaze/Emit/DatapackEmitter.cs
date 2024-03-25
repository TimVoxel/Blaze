using Blaze.Binding;
using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Text;

namespace Blaze.Emit
{
    internal class DatapackEmitter
    {
        private const string TEMP = ".temp";
        private const string RETURN_TEMP_NAME = ".returnTemp";

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
            var bodyBuidler = new StringBuilder();
            var children = ImmutableArray.CreateBuilder<FunctionEmittion>();
            EmitStatement(bodyBlock, bodyBuidler, children);
            return new FunctionEmittion(function.Name, bodyBuidler.ToString(), children.ToImmutable());
        }

        private void EmitStatement(BoundStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    EvaluateBlockStatement((BoundBlockStatement)node, body, children);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement)node, body, children);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EmitVariableDeclarationStatement((BoundVariableDeclarationStatement)node, body, children);
                    break;
                case BoundNodeKind.IfStatement:
                    EvaluateIfStatement((BoundIfStatement)node, body, children);
                    break;
                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement)node, body, children);
                    break;
                case BoundNodeKind.DoWhileStatement:
                    EvaluateDoWhileStatement((BoundDoWhileStatement)node, body, children);
                    break;
                case BoundNodeKind.BreakStatement:
                    EmitBreakStatement((BoundBreakStatement)node, body, children);
                    break;
                case BoundNodeKind.ContinueStatement:
                    EmitContinueStatement((BoundContinueStatement)node, body, children);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private void EvaluateBlockStatement(BoundBlockStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //TEMP
            foreach (BoundStatement statement in node.Statements)
            {
                EmitStatement(statement, bodyBuilder, children);
            }
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //Can be either call or assignment
            var expression = node.Expression;

            if (expression is BoundAssignmentExpression assignment)
            {
                EmitAssignmentExpression(assignment, body, children);
            }
            else if (expression is BoundCallExpression call)
            {
                EmitCallExpression(call, body, children);
            }
            else
            {
                throw new Exception($"Unexpected expression statement kind {expression.Kind}");
            }
        }

        private void EmitVariableDeclarationStatement(BoundVariableDeclarationStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            var assignment = new BoundAssignmentExpression(node.Variable, node.Initializer);
            EmitAssignmentExpression(assignment, body, children);
        }

        private void EvaluateIfStatement(BoundIfStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EvaluateWhileStatement(BoundWhileStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EvaluateDoWhileStatement(BoundDoWhileStatement node, StringBuilder bodyBuilder, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EmitContinueStatement(BoundContinueStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EmitBreakStatement(BoundBreakStatement node, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            throw new NotImplementedException();
        }

        private void EmitCallExpression(BoundCallExpression call, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //Can be a built-in function -> EmitBuildInFunction();
            //Can be a user defined function ->
            //
            //Assign every parameter to temp variable
            //function <function>
            //Reset every parameter

            var isBuiltIt = BuiltInFunctionEmitter.TryEmitBuiltInFunction(call, body, children);
            if (!isBuiltIt)
            {
                var setNames = new Dictionary<ParameterSymbol, string>();

                for (int i = 0; i < call.Arguments.Count(); i++)
                {
                    var argument = call.Arguments[i];
                    var parameter = call.Function.Parameters[i];
                    var paramName = EmitAssignmentExpression(parameter, argument, body, children);
                    setNames.Add(parameter, paramName);
                }

                var functionName = call.Function.Name;
                var command = $"function ns:{functionName}";
                body.AppendLine(command);

                foreach (var parameter in setNames.Keys)
                {
                    var name = setNames[parameter];

                    if (parameter.Type == TypeSymbol.Int || parameter.Type == TypeSymbol.Bool)
                    {
                        var cleanUpCommand = $"scoreboard players reset {name} vars";
                        body.AppendLine(cleanUpCommand);
                    }
                    else
                    {
                        var storageName = parameter.Type == TypeSymbol.String ? "strings" : "objects";
                        var cleanUpCommand = $"data remove storage {storageName} {name}";
                        body.AppendLine(cleanUpCommand);
                    }
                }
            }
        }

        private string EmitAssignmentExpression(BoundAssignmentExpression assignment, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children) 
            => EmitAssignmentExpression(assignment.Variable, assignment.Expression, body, children);

        private string EmitAssignmentExpression(VariableSymbol variable, BoundExpression expression, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //TODO: Add vars scoreboard in a load function
            var name = $"*{variable.Name}";

            if (expression is BoundLiteralExpression l)
            {
                EmitLiteralAssignment(name, l, body, children);
            }
            else if (expression is BoundVariableExpression v)
            {
                EmitVariableAssignment(name, v.Variable, body, children);
            }
            else if (expression is BoundAssignmentExpression a)
            {
                EmitAssignmentExpression(a, body, children);
                EmitVariableAssignment(name, a.Variable, body, children);
            }
            else if (expression is BoundUnaryExpression u)
            {
                EmitUnaryExpressionAssignment(variable, u, body, children);
            }
            else if (expression is BoundBinaryExpression b)
            {
                EmitBinaryExpressionAssignment(variable, b, body, children);
            }
            else if (expression is BoundCallExpression c)
            {
                EmitCallExpressionAssignment(name, c, body, children);
            }
            else if (expression is BoundConversionExpression conv)
            {
                EmitConversionExpressionAssignment(name, conv, body, children);
            }
            else
            {
                body.AppendLine("#Unsupported expression type");
            }
            return name;
        }

        private void EmitLiteralAssignment(string varName, BoundLiteralExpression literal, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //int literal       -> scoreboard players set *v integers <value>
            //string literal    -> data modify storage strings string <SOURCE> [<sourcePath>]
            //bool literal      -> scoreboard players set *v bools <value>

            if (literal.Type == TypeSymbol.String)
            {
                var value = (string)literal.Value;
                var command = $"data modify storage strings {varName} set value \"{value}\"";
                body.AppendLine(command);
            }
            else
            {
                var value = literal.Type == TypeSymbol.Int ? (int)literal.Value
                                    : ((bool)literal.Value ? 1 : 0);

                var command = $"scoreboard players set {varName} vars {value}";
                body.AppendLine(command);
            }
        }

        private void EmitVariableAssignment(string varName, VariableSymbol otherVar, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //int, bool literal -> scoreboard players operation *this vars = *other vars
            //string literal    -> data modify storage strings *this set from storage strings *other

            var other = $"*{otherVar.Name}";

            if (otherVar.Type == TypeSymbol.String || otherVar.Type == TypeSymbol.Object)
            {
                var storageName = otherVar.Type == TypeSymbol.String ? "strings" : "objects";
                var command = $"data modify storage {storageName} {varName} set from storage {storageName} {other}";
                body.AppendLine(command);
            }
            else
            {
                var command = $"scoreboard players operation {varName} vars = {other} vars";
                body.AppendLine(command);
            }
        }

        private void EmitUnaryExpressionAssignment(VariableSymbol variable, BoundUnaryExpression unary, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //TODO: Add constant assignment to load function

            //Identity -> Assign the expression normally
            //Negation -> Assign the expression normally, than multiply it by -1
            //Logical negation
            //         -> Assign the expression to <.temp> variable
            //            If it is 1, set the <*name> to 0
            //            If it is 0, set the <*name> to 1

            var expression = unary.Operand;
            var operatorKind = unary.Operator.OperatorKind;
            var name = $"*{variable.Name}";

            switch (operatorKind)
            {
                case BoundUnaryOperatorKind.Identity:
                    EmitAssignmentExpression(variable, expression, body, children);
                    break;
                case BoundUnaryOperatorKind.Negation:

                    var varName = EmitAssignmentExpression(variable, expression, body, children);
                    var command = $"scoreboard players operation {varName} vars *= *-1 CONST";
                    body.AppendLine(command);
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:

                    var temp = new LocalVariableSymbol(TEMP, variable.Type, null);
                    var tempName = EmitAssignmentExpression(temp, expression, body, children);
                    var command1 = $"execute if score {tempName} vars matches 1 run scoreboard players set {name} vars 0";
                    var command2 = $"execute if score {tempName} vars matches 0 run scoreboard players set {name} vars 1";
                    var cleanUpCommand = $"scoreboard players reset {tempName} vars";
                    body.AppendLine(command1);
                    body.AppendLine(command2);
                    body.AppendLine(cleanUpCommand);
                    break;
                default:
                    throw new Exception($"Unexpected unary operator kind {operatorKind}");
            }
        }

        private void EmitBinaryExpressionAssignment(VariableSymbol variable, BoundBinaryExpression binary, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
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


            var left = binary.Left;
            var right = binary.Right;
            var operatorKind = binary.Operator.OperatorKind;
            var name = $"*{variable.Name}";

            switch (operatorKind)
            {
                case BoundBinaryOperatorKind.Addition:

                    if (left.Type == TypeSymbol.Int)
                    {
                        EmitIntBinaryOperation(variable, body, children, left, right, "+=");
                    }
                    else
                    {
                        body.AppendLine("#String concatination is currently unsupported");
                    }
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    EmitIntBinaryOperation(variable, body, children, left, right, "-=");
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    EmitIntBinaryOperation(variable, body, children, left, right, "*=");
                    break;
                case BoundBinaryOperatorKind.Division:
                    EmitIntBinaryOperation(variable, body, children, left, right, "/=");
                    break;
                case BoundBinaryOperatorKind.LogicalMultiplication:
                    {
                        var leftTemp = new LocalVariableSymbol(".lTemp", left.Type, null);
                        var rightTemp = new LocalVariableSymbol(".rTemp", right.Type, null);

                        var leftName = EmitAssignmentExpression(leftTemp, left, body, children);
                        var rightName = EmitAssignmentExpression(rightTemp, right, body, children);
                        var command1 = $"scoreboard players set {name} vars 0";
                        var command2 = $"execute if score {leftName} vars matches 1 if score {rightName} vars matches 1 run scoreboard players set {name} vars 1";
                        var cleanup1 = $"scoreboard players reset {leftName} vars";
                        var cleanup2 = $"scoreboard players reset {rightName} vars";
                        body.AppendLine(command1);
                        body.AppendLine(command2);
                        body.AppendLine(cleanup1);
                        body.AppendLine(cleanup2);
                    } 
                    break;
                case BoundBinaryOperatorKind.LogicalAddition:
                    {
                        var leftTemp = new LocalVariableSymbol(".lTemp", left.Type, null);
                        var rightTemp = new LocalVariableSymbol(".rTemp", right.Type, null);

                        var leftName = EmitAssignmentExpression(leftTemp, left, body, children);
                        var rightName = EmitAssignmentExpression(rightTemp, right, body, children);
                        var command1 = $"scoreboard players set {name} vars 0";
                        var command2 = $"execute if score {leftName} vars matches 1 run scoreboard players set {name} vars 1";
                        var command3 = $"execute if score {rightName} vars matches 1 run scoreboard players set {name} vars 1";
                        var cleanup1 = $"scoreboard players reset {leftName} vars";
                        var cleanup2 = $"scoreboard players reset {rightName} vars";
                        body.AppendLine(command1);
                        body.AppendLine(command2);
                        body.AppendLine(command3);
                        body.AppendLine(cleanup1);
                        body.AppendLine(cleanup2);
                    }
                    break;
                case BoundBinaryOperatorKind.Equals:
                    if (left.Type == TypeSymbol.String)
                    {
                        var leftTemp = new LocalVariableSymbol("lTemp", left.Type, null);
                        var rightTemp = new LocalVariableSymbol("rTemp", right.Type, null);
                        var leftName = EmitAssignmentExpression(leftTemp, left, body, children);
                        var rightName = EmitAssignmentExpression(rightTemp, right, body, children);

                        var command1 = $"execute store success score {TEMP} vars run data modify storage strings {leftName} set from storage strings {rightName}";
                        var command2 = $"execute if score {TEMP} vars matches 1 run scoreboard players set {name} vars 0";
                        var command3 = $"execute if score {TEMP} vars matches 0 run scoreboard players set {name} vars 1";
                        var cleanup1 = $"scoreboard players reset {leftName} vars";
                        var cleanup2 = $"scoreboard players reset {rightName} vars";
                        var cleanup3 = $"scoreboard players reset {TEMP} vars";
                        body.AppendLine(command1);
                        body.AppendLine(command2);
                        body.AppendLine(command3);
                        body.AppendLine(cleanup1);
                        body.AppendLine(cleanup2);
                        body.AppendLine(cleanup3);
                    }
                    else
                    {
                        EmitComparisonBinaryOperation(body, children, left, right, name, "=");
                    }
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    if (left.Type == TypeSymbol.String)
                    {
                        var leftTemp = new LocalVariableSymbol("lTemp", left.Type, null);
                        var rightTemp = new LocalVariableSymbol("rTemp", right.Type, null);
                        var leftName = EmitAssignmentExpression(leftTemp, left, body, children);
                        var rightName = EmitAssignmentExpression(rightTemp, right, body, children);

                        var command1 = $"execute store success score {name} vars run data modify storage strings {leftName} set from storage strings {rightName}";
                        var cleanup1 = $"scoreboard players reset {leftName} vars";
                        var cleanup2 = $"scoreboard players reset {rightName} vars";
                        body.AppendLine(command1);
                        body.AppendLine(cleanup1);
                        body.AppendLine(cleanup2);
                    }
                    else
                    {
                        EmitComparisonBinaryOperation(body, children, left, right, name, "=", true);
                    }
                    break;
                case BoundBinaryOperatorKind.Less:
                    EmitComparisonBinaryOperation(body, children, left, right, name, "<");
                    break;
                case BoundBinaryOperatorKind.LessOrEquals:
                    EmitComparisonBinaryOperation(body, children, left, right, name, "<=");
                    break;
                case BoundBinaryOperatorKind.Greater:
                    EmitComparisonBinaryOperation(body, children, left, right, name, ">");
                    break;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    EmitComparisonBinaryOperation(body, children, left, right, name, ">=");
                    break;
            }
        }

        private void EmitComparisonBinaryOperation(StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children, BoundExpression left, BoundExpression right, string name, string sign, bool inverted = false)
        {
            var leftTemp = new LocalVariableSymbol(".lTemp", left.Type, null);
            var rightTemp = new LocalVariableSymbol(".rTemp", right.Type, null);
            var leftName = EmitAssignmentExpression(leftTemp, left, body, children);
            var rightName = EmitAssignmentExpression(rightTemp, right, body, children);

            var initialValue = inverted ? 1 : 0;
            var successValue = inverted ? 0 : 1;

            var command1 = $"scoreboard players set {name} vars {initialValue}";
            var command2 = $"execute if score {leftName} vars {sign} {rightName} vars run scoreboard players set {name} vars {successValue}";
            var cleanup1 = $"scoreboard players reset {leftName} vars";
            var cleanup2 = $"scoreboard players reset {rightName} vars";
            body.AppendLine(command1);
            body.AppendLine(command2);
            body.AppendLine(cleanup1);
            body.AppendLine(cleanup2);
        }

        private void EmitIntBinaryOperation(VariableSymbol variable, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children, BoundExpression left, BoundExpression right, string operation)
        {
            var rightTemp = new LocalVariableSymbol(".rTemp", right.Type, null);
            var leftName = EmitAssignmentExpression(variable, left, body, children);
            var rightName = EmitAssignmentExpression(rightTemp, right, body, children);

            var command = $"scoreboard players operation {leftName} vars {operation} {rightName} vars";
            var cleanUpCommand = $"scoreboard players reset {rightName} vars";

            body.AppendLine(command);
            body.AppendLine(cleanUpCommand);
        }

        private void EmitCallExpressionAssignment(string name, BoundCallExpression call, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //The return value via a temp variable also works for int and bool, but since
            //Mojang's added /return why not use it instead

            //1. INT: execute store result <*name> vars run function ...
            //2. BOOL: execute store result <*name> vars run function ...
            //3. STRING: data modify storage strings <*name> set from storage strings <*return>

            if (call.Function.ReturnType == TypeSymbol.Int || call.Function.ReturnType == TypeSymbol.Bool)
            {
                var command = $"execute store result score {name} vars run function ns:{call.Function.Name}";
                body.AppendLine(command);
            }
            else
            {
                var command1 = $"function ns:{call.Function.Name}";
                var command2 = $"data modify storage strings {name} set from storage strings {RETURN_TEMP_NAME}";
                body.Append(command1);
                body.Append(command2);
            }
        }

        private void EmitConversionExpressionAssignment(string name, BoundConversionExpression conv, StringBuilder body, ImmutableArray<FunctionEmittion>.Builder children)
        {
            //to int -> scoreboard players operation
            //to string -> copy to storage, than copy with data modify ... string
            //to object -> data modify storage

            var resultType = conv.Type;
            var sourceType = conv.Expression.Type;

            var temp = new LocalVariableSymbol("temp", sourceType, null);
            var tempName = EmitAssignmentExpression(temp, conv.Expression, body, children);

            if (resultType == TypeSymbol.String && sourceType != TypeSymbol.String && sourceType != TypeSymbol.Object)
            {
                var tempPath = "*temp1";
                var command1 = $"execute store result storage strings {tempPath} int 1 run scoreboard players get {tempName} vars";
                var command2 = $"data modify storage strings {name} set string storage strings {tempPath}";
                var cleanup1 = $"data remove storage strings {tempPath}";
                var cleanup2 = $"scoreboard players reset {tempName} vars";
                body.AppendLine(command1);
                body.AppendLine(command2);
                body.AppendLine(cleanup1);
                body.AppendLine(cleanup2);
            }
            if (resultType == TypeSymbol.Object)
            {
                if (sourceType == TypeSymbol.Int || sourceType == TypeSymbol.Bool)
                {
                    var command = $"execute store result storage objects {name} int 1 run scoreboard players get {tempName} vars";
                    var cleanup = $"scoreboard players reset {tempName} vars";
                    body.AppendLine(command);
                    body.AppendLine(cleanup);
                }
                else
                {
                    var command = $"data modify storage objects {name} set from storage strings {tempName}";
                    var cleanup = $"data remove storage strings {tempName}";
                    body.AppendLine(command);
                    body.AppendLine(cleanup);
                }
            }
        }
    }
}
