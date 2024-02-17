using Blaze.Binding;

namespace Blaze.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public TypeSymbol Type { get; private set; }
        internal BoundConstant? Constant { get; private set; }

        internal VariableSymbol(string name, TypeSymbol type, BoundConstant? constant) : base(name)
        {
            Type = type;
            Constant = constant;
        }
    }

    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.GlobalVariable;
        internal GlobalVariableSymbol(string name, TypeSymbol type, BoundConstant? constant) : base(name, type, constant) { }
    }

    public class LocalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.LocalVariable;
        internal LocalVariableSymbol(string name, TypeSymbol type, BoundConstant? constant) : base(name, type, constant) { }
    }
}
