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

        private void EmitRunCommand(BoundCallExpression call, FunctionEmittion emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.command", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AppendMacro("$(command)");

            emittion.AppendLine($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");
            EmitMacroCleanUp(emittion);
        } 

        private void EmitDatapackEnable(BoundCallExpression call, FunctionEmittion emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.pack", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AppendMacro($"datapack enable \"file/$(pack)\"");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";
            emittion.AppendLine(command);
            EmitMacroCleanUp(emittion);
        }

        private void EmitDatapackDisable(BoundCallExpression call, FunctionEmittion emittion)
        {
            var argument = call.Arguments[0];
            var tempName = EmitAssignmentExpression("**macros.pack", argument, emittion, 0);
            var macro = GetOrCreateBuiltIn(call.Function, out bool isCreated);

            if (isCreated)
                macro.AppendMacro($"datapack disable \"file/$(pack)\"");

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";
            emittion.AppendLine(command);
            EmitMacroCleanUp(emittion);
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
                macro.AppendMacro($"execute if score {valueName} {Vars} matches 1 run return run datapack enable \"file/$(pack)\"");
                macro.AppendMacro($"datapack disable \"file/$(pack)\"");
            }

            var command = $"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros";
            emittion.AppendLine(command);

            EmitCleanUp(valueName, value.Type, emittion);
            EmitMacroCleanUp(emittion);
        }
        
        private void EmitGetDatapackCount(string name, BoundCallExpression call, FunctionEmittion emittion, bool countEnabled = false, bool countAvailable = false)
        {
            string filter = string.Empty;
            if (countEnabled)
                filter = "enabled";
            else
                filter = "available";

            var command = $"execute store result score {name} {Vars} run datapack list {filter}";
            emittion.AppendLine(command);
        } 

        private void EmitSetWeather(BoundCallExpression call, FunctionEmittion emittion, int current, string? timeUnits = null)
        { 
            void EmitNonMacroNonConstantTypeCheck(BoundExpression weatherType, FunctionEmittion emittion, int current, int time = 0, string? timeUnits = null)
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

                    emittion.AppendLine(command);
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
                        emittion.AppendLine($"weather {em.Name.ToLower()} {l.Value}{timeUnits}");
                        return;
                    }
                    else
                    {
                        var time = (int) l.Value;
                        EmitNonMacroNonConstantTypeCheck(weatherType, emittion, current, time, timeUnits);
                    }
                }
                else 
                {
                    var type = EmitAssignmentExpression("**macros.type", weatherType, emittion, current);
                    var duration = EmitAssignmentToTemp("dur", call.Arguments[1], emittion, current);
                    var macro = GetOrCreateBuiltIn(BuiltInNamespace.Minecraft.General.Weather.SetWeather, out bool isCreated);

                    emittion.AppendLine($"execute store result storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.duration int 1 run scoreboard players get {duration} {Vars}");
                    emittion.AppendLine($"data modify storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros.tu set value \"{timeUnits}\"");
                    emittion.AppendLine($"function {_nameTranslator.GetCallLink(macro)} with storage {_nameTranslator.GetStorage(TypeSymbol.String)} **macros");

                    if (isCreated)
                        macro.AppendMacro($"weather $(type) $(duration)(tu)");

                    EmitCleanUp(duration, TypeSymbol.Int, emittion);
                    EmitMacroCleanUp(emittion);
                }
            }
            else
            {
                //Don't have time specified
                if (weatherType is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
                {
                    emittion.AppendLine($"weather {em.Name.ToLower()}");
                }
                else
                {
                    EmitNonMacroNonConstantTypeCheck(weatherType, emittion, current);
                }  
            }
        }

        private void EmitSetWeatherForTicks(BoundCallExpression call, FunctionEmittion emittion, int current)
            => EmitSetWeather(call, emittion, current, "t");

        private void EmitSetWeatherForSeconds(BoundCallExpression call, FunctionEmittion emittion, int current)
            => EmitSetWeather(call, emittion, current, "s");

        private void EmitSetWeatherForDays(BoundCallExpression call, FunctionEmittion emittion, int current)
            => EmitSetWeather(call, emittion, current, "d");

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
                command = "tellraw @a {\"storage\":\"{_nameTranslator.GetStorage(TypeSymbol.String)}\",\"nbt\":\"\\\"" + varName + "\\\"\"}";
            }
            else
            {
                var tempName = EmitAssignmentToTemp(TEMP, argument, emittion, 0, false);
                command = "tellraw @a {\"storage\":\"{_nameTranslator.GetStorage(TypeSymbol.String)}\",\"nbt\":\"\\\"" + tempName + "\\\"\"}";
                EmitCleanUp(tempName, argument.Type, emittion);
            }
            
            emittion.AppendLine(command);
        }
    }
}
