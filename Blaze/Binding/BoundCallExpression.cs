using System.Collections.Immutable;
using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public FunctionSymbol Function { get; private set; }
        public ImmutableArray<BoundExpression> Arguments { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.ReturnType;

        public BoundCallExpression(FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
        {
            Function = function;
            Arguments = arguments;
        }
    }
}