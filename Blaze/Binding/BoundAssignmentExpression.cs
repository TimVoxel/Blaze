using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundAssignmentExpression : BoundExpression
    {
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }

        public override TypeSymbol Type => Right.Type;
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;

        internal BoundAssignmentExpression(BoundExpression left, BoundExpression right)
        {
            Left = left;
            Right = right;
        }
    }
}