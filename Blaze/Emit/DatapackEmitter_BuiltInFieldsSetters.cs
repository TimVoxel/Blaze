﻿using Blaze.Binding;
using Blaze.Symbols;
using Blaze.Symbols.BuiltIn;
using System.Diagnostics;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public bool TryEmitBuiltInFieldAssignment(FieldSymbol field, BoundExpression right, FunctionEmittion emittion, int current, out string? tempName)
        {
            if (MinecraftNamespace.GeneralNamespace.GamerulesNamespace.IsGamerule(field))
            {
                tempName = EmitGameruleAssignment(field, right, emittion, current);
                return true;
            }
            tempName = null;
            return false;
        }

        private string EmitGameruleAssignment(FieldSymbol field, BoundExpression right, FunctionEmittion emittion, int current)
        {
            //1. Evaluate right to temp
            //2. If is bool, generate two conditions
            //3. If it's an int, generate a macro

            var rightName = EmitAssignmentToTemp(right, emittion, current);

            if (field.Type == TypeSymbol.Bool)
            {
                var command1 = $"execute if score {rightName} vars matches 1 run gamerule {field.Name} true";
                var command2 = $"execute if score {rightName} vars matches 0 run gamerule {field.Name} false";
                emittion.AppendLine(command1);
                emittion.AppendLine(command2);
            }
            else
            {
                var macroFunctionSymbol = MinecraftNamespace.GeneralNamespace.GamerulesNamespace.SetGamerule;
                Debug.Assert(macroFunctionSymbol != null);
                var macro = GetOrCreateBuiltIn(macroFunctionSymbol, out bool isCreated);

                var command1 = $"data modify storage strings **macros.rule set value \"{field.Name}\"";
                var command2 = $"execute store result storage strings **macros.value int 1 run scoreboard players get {rightName} vars";
                var command3 = $"function {_nameTranslator.GetCallLink(macro)} with storage strings **macros";

                if (isCreated)
                    macro.AppendMacro("gamerule $(rule) $(value)");

                EmitCleanUp("**macros.value", TypeSymbol.String, emittion);
                EmitCleanUp("**macros.rule", TypeSymbol.String, emittion);
                emittion.AppendLine(command1);
                emittion.AppendLine(command2);
                emittion.AppendLine(command3);
            }

            EmitCleanUp(rightName, right.Type, emittion);
            return rightName;
        }
    }
}
