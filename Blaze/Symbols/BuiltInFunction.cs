using System.Collections.Immutable;

namespace Blaze.Symbols
{
    internal static class BuiltInFunction
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Object)), TypeSymbol.Void);
        public static readonly FunctionSymbol Random = new FunctionSymbol("rand", ImmutableArray.Create(new ParameterSymbol("origin", TypeSymbol.Int), new ParameterSymbol("bound", TypeSymbol.Int)), TypeSymbol.Int);
        public static readonly FunctionSymbol RunCommand = new FunctionSymbol("run_command", ImmutableArray.Create(new ParameterSymbol("command", TypeSymbol.String)), TypeSymbol.Void);
        
        internal static IEnumerable<FunctionSymbol> GetAll()
        {
            yield return Print;
            yield return RunCommand;
        }
    }   
}
