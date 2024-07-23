using Blaze.Symbols;

namespace Blaze.Binding
{
    internal class BoundCompoundAssignmentExpression : BoundExpression
    {
        public BoundExpression Left { get; }
        public BoundBinaryOperator Operator { get; }
        public BoundExpression Right { get; }

        public override TypeSymbol Type => Right.Type;
        public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;

        internal BoundCompoundAssignmentExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }
}
