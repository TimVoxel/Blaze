using Blaze.Binding;
using Blaze.Symbols;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public bool TryEmitBuiltInFunction(BoundCallExpression call, FunctionEmittion emittion)
        {
            if (call.Function == BuiltInNamespace.Minecraft.Chat.Say)
            {
                EmitSay(call, emittion);
                return true;
            }
            else if (call.Function == BuiltInNamespace.Minecraft.Chat.Print)
            {
                EmitPrint(call, emittion);
                return true;
            }
            return false;
        }

        private void EmitSay(BoundCallExpression call, FunctionEmittion emittion) => EmitPrint(call, emittion);

        private void EmitPrint(BoundCallExpression call, FunctionEmittion emittion)
        {
            var expression = call.Arguments[0];
            var command = string.Empty;

            if (expression is BoundLiteralExpression literal)
            {
                command = "tellraw @a {\"text\":\"" + literal.Value + "\"}";
            }
            else if (expression is BoundVariableExpression variable)
            {
                var varName = _nameTranslator.GetVariableName(variable.Variable);
                command = "tellraw @a {\"storage\":\"strings\",\"nbt\":\"" + varName + "\"}";
            }
            else
            {
                var tempName = EmitAssignmentToTemp(TEMP, expression, emittion, 0, false);
                command = "tellraw @a {\"storage\":\"strings\",\"nbt\":\"" + tempName + "\"}";
                EmitCleanUp(tempName, call.Arguments[0].Type, emittion);
            }
            
            emittion.AppendLine(command);
        }
    }
}
