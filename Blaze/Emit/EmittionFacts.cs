using Blaze.Binding;
using Blaze.Symbols;
using static Blaze.Emit.Nodes.ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause;
using Blaze.Emit.Nodes.Execute;
using System.Text;

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

        public static string GetSyntaxName(this DataLocation emittionVariableLocation)
        {
            return emittionVariableLocation switch
            {
                DataLocation.Scoreboard => "score",
                DataLocation.Storage => "storage",
                _ => throw new Exception($"Unexpected variable location {emittionVariableLocation}")
            };
        }

        public static string GetSyntaxName(this PositionedOverExecuteSubCommand.HeightMapType heightMap)
        {
            return heightMap switch
            {
                PositionedOverExecuteSubCommand.HeightMapType.WorldSurface => "world_surface",
                PositionedOverExecuteSubCommand.HeightMapType.OceanFloor => "ocean_floor",
                PositionedOverExecuteSubCommand.HeightMapType.MotionBlockNoLeaves => "motion_block_no_leaves",
                PositionedOverExecuteSubCommand.HeightMapType.MotionBlocking => "motion_blocking",
                _ => throw new Exception($"Unexpected height map type {heightMap}")
            };
        }

        public static string GetSign(this IfScoreClause.ComparisonType comparison)
        {
            return comparison switch
            {
                IfScoreClause.ComparisonType.Greater => ">",
                IfScoreClause.ComparisonType.GreaterOrEquals => ">=",
                IfScoreClause.ComparisonType.Less => "<",
                IfScoreClause.ComparisonType.LessOrEquals => "<=",
                IfScoreClause.ComparisonType.Equals => "=",
                _ => throw new Exception($"Unexpectefd comparison type {comparison}")
            };
        }

        internal static IfScoreClause.ComparisonType ToComparisonType(BoundBinaryOperatorKind kind)
        {
            return kind switch
            {
                BoundBinaryOperatorKind.GreaterOrEquals => IfScoreClause.ComparisonType.GreaterOrEquals,
                BoundBinaryOperatorKind.Greater => IfScoreClause.ComparisonType.Greater,
                BoundBinaryOperatorKind.Equals => IfScoreClause.ComparisonType.Equals,
                BoundBinaryOperatorKind.Less => IfScoreClause.ComparisonType.Less,
                BoundBinaryOperatorKind.LessOrEquals => IfScoreClause.ComparisonType.LessOrEquals,
                _ => throw new Exception($"Unexpected binary operator kind {kind}")
            };
        }

        internal static StoreExecuteSubCommand.StoreType TypeToStoreType(TypeSymbol type)
        {
            if (type == TypeSymbol.Int || type == TypeSymbol.Bool)
                return StoreExecuteSubCommand.StoreType.Int;
            if (type == TypeSymbol.Float)
                return StoreExecuteSubCommand.StoreType.Float;
            if (type == TypeSymbol.Double)
                return StoreExecuteSubCommand.StoreType.Double;

            return StoreExecuteSubCommand.StoreType.Int;
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

        internal static string GetSign(this PlayersOperation operation)
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

        public static string GetRangeString(string? lowerBound, string? upperBound)
        {
            var builder = new StringBuilder();
            if (lowerBound != null)
                builder.Append(lowerBound);

            if (upperBound != null)
            {
                if (upperBound == lowerBound)
                    return builder.ToString();

                builder.Append("..");
                builder.Append(upperBound);
            }
            else
                builder.Append("..");

            return builder.ToString();
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
}
