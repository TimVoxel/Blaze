using Blaze.Binding;
using Blaze.Symbols;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public bool TryEmitBuiltInFunction(string? varName, BoundCallExpression call, FunctionEmittion emittion, int current)
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

        private void EmitRunCommand(BoundCallExpression call, FunctionEmittion emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.command", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AppendMacro("$(command)");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage strings **macros";
            emittion.AppendLine(command);
            EmitCleanUp(tempName, argument.Type, emittion);
        } 

        private void EmitDatapackEnable(BoundCallExpression call, FunctionEmittion emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.pack", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AppendMacro($"datapack enable \"file/$(pack)\"");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage strings **macros";
            emittion.AppendLine(command);
            EmitCleanUp(tempName, argument.Type, emittion);
        }

        private void EmitDatapackDisable(BoundCallExpression call, FunctionEmittion emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.pack", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AppendMacro($"datapack disable \"file/$(pack)\"");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage strings **macros";
            emittion.AppendLine(command);
            EmitCleanUp(tempName, argument.Type, emittion);
        }

        private void EmitSetDatapackEnabled(BoundCallExpression call, FunctionEmittion emittion, int current)
        {
            var pack = call.Arguments[0];
            var value = call.Arguments[1];

            var packName = EmitAssignmentExpression("**macros.pack", pack, emittion, current);
            var valueName = EmitAssignmentToTemp(TEMP, value, emittion, current, false);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
            {
                macro.AppendMacro($"execute if score {valueName} vars matches 1 run return run datapack enable \"file/$(pack)\"");
                macro.AppendMacro($"datapack disable \"file/$(pack)\"");
            }

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage strings **macros";
            emittion.AppendLine(command);

            EmitCleanUp(packName, pack.Type, emittion);
            EmitCleanUp(valueName, value.Type, emittion);
        }
        
        private void EmitGetDatapackCount(string name, BoundCallExpression call, FunctionEmittion emittion, bool countEnabled = false, bool countAvailable = false)
        {
            string filter = string.Empty;
            if (countEnabled)
                filter = " enabled";
            else
                filter = " available";

            var command = $"execute store result score {name} vars run datapack list{filter}";
            emittion.AppendLine(command);
        } 

        private void EmitSay(BoundCallExpression call, FunctionEmittion emittion) => EmitPrint(call, emittion);

        private void EmitPrint(BoundCallExpression call, FunctionEmittion emittion)
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
                command = "tellraw @a {\"storage\":\"strings\",\"nbt\":\"\\\"" + varName + "\\\"\"}";
            }
            else
            {
                var tempName = EmitAssignmentToTemp(TEMP, argument, emittion, 0, false);
                command = "tellraw @a {\"storage\":\"strings\",\"nbt\":\"\\\"" + tempName + "\\\"\"}";
                EmitCleanUp(tempName, argument.Type, emittion);
            }
            
            emittion.AppendLine(command);
        }
    }
}
