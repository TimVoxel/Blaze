namespace DPP_Compiler.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
        public override Type Type => Variable.Type;

        public BoundVariableExpression(VariableSymbol symbol)
        {
            Variable = symbol;
        }
    }
}
