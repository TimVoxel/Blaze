using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundThisExpression : BoundExpression
    {
        public override TypeSymbol Type { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ThisExpression;

        public BoundThisExpression(NamedTypeSymbol type)
        {
            Type = type;
        }
    }
}