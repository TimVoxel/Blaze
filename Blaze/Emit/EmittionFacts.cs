using Blaze.Binding;
using Blaze.Symbols;
using static Blaze.Symbols.EmittionVariableSymbol;
using static Blaze.Emit.Nodes.ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause;

namespace Blaze.Emit
{
    public static class EmittionFacts
    {
        public static DataLocation ToLocation(TypeSymbol type)
        {
            if (type is ArrayTypeSymbol)
            {
                return DataLocation.Storage;
            }
            if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol e && e.IsIntEnum)
            {
                return DataLocation.Scoreboard;
            }
            else
            {
                return DataLocation.Storage;
            }
        }

        public static string GetLocationSyntaxName(DataLocation emittionVariableLocation)
        {
            return emittionVariableLocation switch
            {
                DataLocation.Scoreboard => "score",
                DataLocation.Storage => "storage",
                _ => throw new Exception($"Unexpected variable location {emittionVariableLocation}")
            };
        }

        internal static PlayersOperation ToPlayersOperation(BoundBinaryOperatorKind kind)
        {
            return kind switch
            {
                BoundBinaryOperatorKind.Addition => PlayersOperation.Addition,
                BoundBinaryOperatorKind.Subtraction => PlayersOperation.Subtraction,
                BoundBinaryOperatorKind.Multiplication => PlayersOperation.Multiplication,
                BoundBinaryOperatorKind.Division => PlayersOperation.Division,
                _ => throw new Exception($"No scoreboard players operation command corresponds to {kind} operator kind")
            };
        }

        internal static string GetSignText(PlayersOperation operation)
        {
            return operation switch
            {
                PlayersOperation.Assignment => "=",
                PlayersOperation.Addition => "+=",
                PlayersOperation.Subtraction => "-=",
                PlayersOperation.Multiplication => "*=",
                PlayersOperation.Division => "/=",
                PlayersOperation.Mod => "%=",
                PlayersOperation.Min => "<",
                PlayersOperation.Max => ">",
                PlayersOperation.Swap => "><",
                _ => throw new Exception($"No sign corresponds to {operation}")
            };
        }

        internal static string GetScoreboardOperationsOperatorSymbol(BoundBinaryOperatorKind kind)
        {
            return kind switch
            {
                BoundBinaryOperatorKind.Addition => "+=",
                BoundBinaryOperatorKind.Subtraction => "-=",
                BoundBinaryOperatorKind.Multiplication => "*=",
                BoundBinaryOperatorKind.Division => "/=",
                _ => "="
            };
        }

        internal static string GetComparisonSign(BoundBinaryOperatorKind kind)
        {
            return kind switch
            {
                BoundBinaryOperatorKind.Less => "<",
                BoundBinaryOperatorKind.LessOrEquals => "<=",
                BoundBinaryOperatorKind.Greater => ">",
                BoundBinaryOperatorKind.GreaterOrEquals => ">=",
                _ => "="
            };
        }

        public static string GetEmittionDefaultValue(TypeSymbol type)
        {
            if (type is NamedTypeSymbol)
                return "{}";
            if (type is EnumSymbol)
                return "{}";
            if (type == TypeSymbol.Object)
                return "0";
            else if (type == TypeSymbol.Int)
                return "0";
            else if (type == TypeSymbol.Float)
                return "0.0f";
            else if (type == TypeSymbol.Double)
                return "0.0d";
            else if (type == TypeSymbol.Bool)
                return "0";
            else if (type == TypeSymbol.String)
                return "\"\"";

            return "{}";
        }
    }


    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
