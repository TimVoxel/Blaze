using Blaze.Binding;

namespace Blaze.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        public TypeSymbol Type { get; }
        internal BoundConstant? Constant { get; }
        public bool IsReadOnly { get; }

        internal VariableSymbol(string name, TypeSymbol type, bool isReadOnly, BoundConstant? constant) : base(name)
        {
            Type = type;
            Constant = constant;
            IsReadOnly = isReadOnly;
        }
    }

    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.GlobalVariable;
        internal GlobalVariableSymbol(string name, TypeSymbol type, bool isReadOnly, BoundConstant? constant) : base(name, type, isReadOnly, constant) { }
    }

    public class LocalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.LocalVariable;
        internal LocalVariableSymbol(string name, TypeSymbol type, bool isReadOnly, BoundConstant? constant) : base(name, type, isReadOnly, constant) { }
    }
}
