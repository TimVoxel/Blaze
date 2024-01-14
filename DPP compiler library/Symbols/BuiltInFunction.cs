using System.Collections.Immutable;

namespace DPP_Compiler.Symbols
{
    internal static class BuiltInFunction
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String)), TypeSymbol.Void);
        public static readonly FunctionSymbol Input = new FunctionSymbol("input", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);

        internal static IEnumerable<FunctionSymbol> GetAll()
        {
            yield return Print;
            yield return Input;
        }
    }   
}
