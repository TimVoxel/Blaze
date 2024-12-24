using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundArrayCreationExpression : BoundExpression
    {
        public ImmutableArray<BoundExpression> Dimensions { get; }
        public override TypeSymbol Type => ArrayType;
        public ArrayTypeSymbol ArrayType { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ArrayCreationExpression;

        public BoundArrayCreationExpression(ArrayTypeSymbol type, ImmutableArray<BoundExpression> dimensions)
        {
            ArrayType = type;
            Dimensions = dimensions;
        }
    }
}