using Blaze.Symbols;

namespace Blaze.Binding
{
    internal class BoundCompoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol Variable { get; private set; }
        public BoundBinaryOperator Operator { get; }
        public BoundExpression Expression { get; private set; }

        public override TypeSymbol Type => Expression.Type;
        public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;

        internal BoundCompoundAssignmentExpression(VariableSymbol variable, BoundBinaryOperator op, BoundExpression expression)
        {
            Variable = variable;
            Operator = op;
            Expression = expression;
        }
    }
}
