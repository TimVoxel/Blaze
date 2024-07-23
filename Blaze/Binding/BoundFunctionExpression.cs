using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundFunctionExpression : BoundExpression
    {
        public FunctionSymbol Function { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.FunctionExpression;
        public override TypeSymbol Type => TypeSymbol.Function;
        public BoundFunctionExpression(FunctionSymbol function)
        {
            Function = function;
        } 

    }
}