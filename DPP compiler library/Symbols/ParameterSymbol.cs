namespace DPP_Compiler.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.Parameter;

        public ParameterSymbol(string name, TypeSymbol type) : base(name, type) { }
    }
}
