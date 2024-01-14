namespace DPP_Compiler.Symbols
{
    public sealed class ParameterSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.Parameter;

        public ParameterSymbol(string name, TypeSymbol type) : base(name, type) { }
    }
}
