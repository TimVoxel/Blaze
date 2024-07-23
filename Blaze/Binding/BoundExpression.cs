using Blaze.Symbols;

namespace Blaze.Binding
{
    internal abstract class BoundExpression : BoundNode 
    {
        public abstract TypeSymbol Type { get; }
        public virtual BoundConstant? ConstantValue => null;
    }
}