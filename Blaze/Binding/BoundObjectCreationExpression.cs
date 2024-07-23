using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundObjectCreationExpression : BoundExpression
    {
        public NamedTypeSymbol NamedType { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ObjectCreationExpression;
        public override TypeSymbol Type => NamedType;

        public BoundObjectCreationExpression(NamedTypeSymbol namedType, ImmutableArray<BoundExpression> arguments)
        {
            NamedType = namedType;
            Arguments = arguments;
        }
    }
}