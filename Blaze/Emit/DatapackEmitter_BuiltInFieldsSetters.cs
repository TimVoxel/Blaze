using Blaze.Binding;
using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Emit
{
    internal partial class DatapackEmitter
    {
        public bool TryEmitBuiltInFieldAssignment(FieldSymbol field, BoundExpression right, FunctionEmittion emittion, int current, out string? tempName)
        {
            if (BuiltInNamespace.Minecraft.General.Gamerules.IsGamerule(field))
            {
                tempName = EmitGameruleAssignment(field, right, emittion, current);
                return true;
            }
            else if (BuiltInNamespace.Minecraft.General.DifficultyField == field)
            {
                tempName = EmitDifficultyAssignment(field, right, emittion, current);
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
                var macroFunctionSymbol = BuiltInNamespace.Minecraft.General.Gamerules.SetGamerule;
                Debug.Assert(macroFunctionSymbol != null);
                var macro = GetOrCreateBuiltIn(macroFunctionSymbol, out bool isCreated);

                var command1 = $"data modify storage strings **macros.rule set value \"{field.Name}\"";
                var command2 = $"execute store result storage strings **macros.value int 1 run scoreboard players get {rightName} vars";
                var command3 = $"function {_nameTranslator.GetCallLink(macro)} with storage strings **macros";

                if (isCreated)
                    macro.AppendMacro("gamerule $(rule) $(value)");

                emittion.AppendLine(command1);
                emittion.AppendLine(command2);
                emittion.AppendLine(command3);
                EmitMacroCleanUp(emittion);
            }

            EmitCleanUp(rightName, right.Type, emittion);
            return rightName;
        }

        private string EmitDifficultyAssignment(FieldSymbol field, BoundExpression right, FunctionEmittion emittion, int current)
        {
            if (right is BoundVariableExpression variableExpression && variableExpression.Variable is EnumMemberSymbol em)
            {
                var command = $"difficulty {em.Name.ToLower()}";
                emittion.AppendLine(command);
                return string.Empty;
            }

            var rightName = EmitAssignmentToTemp(right, emittion, current);

            foreach (var enumMember in BuiltInNamespace.Minecraft.General.Difficulty.Members)
            {
                var intMember = (IntEnumMemberSymbol) enumMember;
                var command = $"execute if score {rightName} vars matches {intMember.UnderlyingValue} run difficulty {enumMember.Name.ToLower()}";
                emittion.AppendLine(command);
            }

            EmitCleanUp(rightName, right.Type, emittion);
            return rightName;
        }
    }
}
