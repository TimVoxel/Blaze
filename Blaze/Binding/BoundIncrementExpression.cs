using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundIncrementExpression : BoundExpression
    {
        public VariableSymbol Variable { get; private set; }
        public BoundBinaryOperator IncrementOperator { get; }

        public override TypeSymbol Type => Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.IncrementExpression;

        internal BoundIncrementExpression(VariableSymbol variable, BoundBinaryOperator op)
        {
            Variable = variable;
            IncrementOperator = op;
        }
    }
}
