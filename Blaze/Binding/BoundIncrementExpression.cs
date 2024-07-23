using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundIncrementExpression : BoundExpression
    {
        public BoundExpression Operand { get; private set; }
        public BoundBinaryOperator IncrementOperator { get; }

        public override TypeSymbol Type => Operand.Type;
        public override BoundNodeKind Kind => BoundNodeKind.IncrementExpression;

        internal BoundIncrementExpression(BoundExpression operand, BoundBinaryOperator op)
        {
            Operand = operand;
            IncrementOperator = op;
        }
    }
}
