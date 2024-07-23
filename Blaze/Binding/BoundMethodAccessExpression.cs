using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundMethodAccessExpression : BoundExpression
    {
        public BoundExpression Instance { get; }
        public FunctionSymbol Method { get; }

        public override BoundNodeKind Kind => BoundNodeKind.MethodAccessExpression;
        public override TypeSymbol Type => TypeSymbol.Function;

        public BoundMethodAccessExpression(BoundExpression instance, FunctionSymbol method)
        {
            Instance = instance;
            Method = method;
        }
    }
}