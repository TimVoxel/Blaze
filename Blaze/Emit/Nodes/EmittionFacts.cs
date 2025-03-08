using Blaze.Symbols;
using static Blaze.Symbols.EmittionVariableSymbol;

namespace Blaze.Emit.Nodes
{
    public static class EmittionFacts
    {
        public static EmittionVariableLocation ToLocation(TypeSymbol type)
        {
            if (type is ArrayTypeSymbol)
            {
                return EmittionVariableLocation.Storage;
            }
            if (type == TypeSymbol.Int || type == TypeSymbol.Bool || type is EnumSymbol e && e.IsIntEnum)
            {
                return EmittionVariableLocation.Scoreboard;
            }
            else
            {
                return EmittionVariableLocation.Storage;
            }
        }

        public static string GetEmittionDefaultValue(TypeSymbol type)
        {
            if (type is NamedTypeSymbol)
                return "{}";
            if (type is EnumSymbol e)
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
