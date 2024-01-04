namespace Compiler_snapshot.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression 
    {
        public BoundUnaryOperatorKind OperatorKind { get; private set; }
        public BoundExpression Operand { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override Type Type => Operand.Type;


        public BoundUnaryExpression(BoundUnaryOperatorKind operatorKind, BoundExpression operand)
        {
            OperatorKind = operatorKind;
            Operand = operand;
        }
    }
}