using System.Collections.Immutable;

namespace Blaze.Symbols
{
    internal static class BuiltInFunction
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String)), TypeSymbol.Void);
        public static readonly FunctionSymbol Input = new FunctionSymbol("input", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly FunctionSymbol Random = new FunctionSymbol("rand", ImmutableArray.Create(new ParameterSymbol("origin", TypeSymbol.Int), new ParameterSymbol("bound", TypeSymbol.Int)), TypeSymbol.Int);
        
        internal static IEnumerable<FunctionSymbol> GetAll()
        {
            yield return Print;
            yield return Input;
            yield return Random;
        }
    }   
}
