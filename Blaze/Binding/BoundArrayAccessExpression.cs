using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal class BoundArrayAccessExpression : BoundExpression
    {
        public BoundExpression Identifier { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ArrayAccessExpression;
        public override TypeSymbol Type { get; }

        public BoundArrayAccessExpression(TypeSymbol type, BoundExpression accessed, ImmutableArray<BoundExpression> arguments)
        {
            Type = type;
            Identifier = accessed;
            Arguments = arguments;
        }
    }
}
