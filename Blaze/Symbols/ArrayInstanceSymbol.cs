using System.Collections.Immutable;

namespace Blaze.Symbols
{
    public sealed class ArrayInstanceSymbol : VariableSymbol
    {
        public new ArrayTypeSymbol Type { get; }
        public ImmutableArray<int> Dimensions { get; }

        public override SymbolKind Kind => SymbolKind.ArrayInstance;

        public ArrayInstanceSymbol(string name, ArrayTypeSymbol type, ImmutableArray<int> dimensions) : base(name, type, false, null)
        {
            Type = type;
            Dimensions = dimensions;
        }
    }
}
