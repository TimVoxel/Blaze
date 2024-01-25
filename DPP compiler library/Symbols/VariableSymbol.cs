namespace DPP_Compiler.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public TypeSymbol Type { get; private set; }

        internal VariableSymbol(string name, TypeSymbol type) : base(name)
        {
            Type = type;
        }
    }

    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.GlobalVariable;
        internal GlobalVariableSymbol(string name, TypeSymbol type) : base(name, type) { }
    }

    public class LocalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.LocalVariable;
        internal LocalVariableSymbol(string name, TypeSymbol type) : base(name, type) { }
    }
}
