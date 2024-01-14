namespace DPP_Compiler.Symbols
{
    public sealed class VariableSymbol : Symbol
    {
        public TypeSymbol Type { get; private set; }

        public override SymbolKind Kind => SymbolKind.Variable;

        internal VariableSymbol(string name, TypeSymbol type) : base(name)
        {
            Type = type;
        }
    }
}
