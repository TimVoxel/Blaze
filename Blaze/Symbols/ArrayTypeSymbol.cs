namespace Blaze.Symbols
{
    public sealed class ArrayTypeSymbol : TypeSymbol
    {
        public int Rank { get; }
        public TypeSymbol Type { get; }

        public override SymbolKind Kind => SymbolKind.ArrayType;

        public ArrayTypeSymbol(TypeSymbol type, int rank) : base(type.Name)
        {
            Rank = rank;
            Type = type;
        }
    }
}
