using Blaze.Binding;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Text;

namespace Blaze.Emit
{
    internal static class BuiltInFunctionEmitter
    {
        public static bool TryEmitBuiltInFunction(BoundCallExpression call, FunctionEmittion emittion)
        {
            if (call.Function == BuiltInFunction.RunCommand)
            {
                var costant = call.Arguments[0].ConstantValue;

                if (costant == null)
                {
                    //HACK
                    throw new Exception($"run_command only uses constant values");
                }

                var message = (string)costant.Value;
                emittion.AppendLine(message);
                return true;

            }
            if (call.Function == BuiltInFunction.Print)
            {
                var message = call.Arguments[0].ToString().Replace("\"", "\\\"");
                var convertedMessage = "{\"text\":\"§e" + message + "\"}";
                emittion.AppendLine($"tellraw @a {convertedMessage}");
                return true;
            }
            return false;
        }
    }
}
