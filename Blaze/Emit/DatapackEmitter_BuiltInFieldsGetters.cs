using Blaze.Binding;
using Blaze.Symbols;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public bool TryEmitBuiltInFieldGetter(string outputName, BoundFieldAccessExpression right, FunctionEmittion emittion, int current)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(right.Field))
            {
                var command = $"execute store result score {outputName} {Vars} run gamerule {right.Field.Name}";
                emittion.AppendLine(command);
                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == right.Field)
            {
                var command = $"execute store result score {outputName} {Vars} run difficulty";
                emittion.AppendLine(command);
                return true;
            }
            return false;
        }

    }
}
