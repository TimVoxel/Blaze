using Blaze.Binding;
using Blaze.Symbols.BuiltIn;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public bool TryEmitBuiltInFieldGetter(string outputName, BoundFieldAccessExpression right, FunctionEmittion emittion, int current)
        {
            if (MinecraftNamespace.GeneralNamespace.GamerulesNamespace.IsGamerule(right.Field))
            {
                var command = $"execute store result score {outputName} vars run gamerule {right.Field.Name}";
                emittion.AppendLine(command);
                return true;
            }
            return false;
        }
    }
}
