namespace DPP_Compiler.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public object Value { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override Type Type => Value.GetType();

        public BoundLiteralExpression(object value)
        {
            Value = value;
        }
    }
}