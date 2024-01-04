namespace Compiler_snapshot.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundExpression Left { get; private set; }
        public BoundBinaryOperatorKind OperatorKind { get; private set; }
        public BoundExpression Right { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override Type Type => Left.Type;


        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperatorKind operatorKind, BoundExpression right)
        {
            Left = left;
            OperatorKind = operatorKind;
            Right = right;
        }
    }
}