using System.Collections.Immutable;
using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public BoundExpression Identifier { get; private set; }
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.ReturnType;

        public BoundCallExpression(BoundExpression identifier, FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
        {
            Identifier = identifier;
            Function = function;
            Arguments = arguments;
        }
    } 
}