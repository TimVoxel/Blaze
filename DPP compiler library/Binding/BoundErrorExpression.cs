using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundErrorExpression : BoundExpression
    {
        public override TypeSymbol Type => TypeSymbol.Error;
        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
        public override IEnumerable<BoundNode> GetChildren() => Enumerable.Empty<BoundNode>();
    }
}