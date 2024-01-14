using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal abstract class BoundExpression : BoundNode
    {
        public abstract TypeSymbol Type { get; }
    }
}