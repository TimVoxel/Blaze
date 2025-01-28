namespace Blaze.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.Parameter;
        public int FunctionHash { get; }

        public ParameterSymbol(string name, TypeSymbol type, int functionHash) : base(name, type, false, null) 
        {
            FunctionHash = functionHash;
        }
    }
}
