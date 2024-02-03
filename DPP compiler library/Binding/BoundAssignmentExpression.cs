using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol Variable { get; private set; }
        public BoundExpression Expression { get; private set; }

        public override TypeSymbol Type => Expression.Type;
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;

        internal BoundAssignmentExpression(VariableSymbol variable, BoundExpression expression)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}