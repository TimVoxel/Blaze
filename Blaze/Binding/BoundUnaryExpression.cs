using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression 
    {
        public BoundUnaryOperator Operator { get; }
        public BoundExpression Operand { get; }

        public override BoundConstant? ConstantValue { get; }
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type => Operand.Type;

        public BoundUnaryExpression(BoundUnaryOperator op, BoundExpression operand)
        {
            Operator = op;
            Operand = operand;
            ConstantValue = ConstantFolding.ComputeConstant(op, operand);
        }
    }
}