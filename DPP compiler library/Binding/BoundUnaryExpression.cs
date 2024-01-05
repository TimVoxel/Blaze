namespace DPP_Compiler.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression 
    {
        public BoundUnaryOperator Operator { get; private set; }
        public BoundExpression Operand { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override Type Type => Operand.Type;


        public BoundUnaryExpression(BoundUnaryOperator op, BoundExpression operand)
        {
            Operator = op;
            Operand = operand;
        }
    }
}