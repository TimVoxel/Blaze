using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundForStatement : BoundLoopStatement
    {
        public VariableSymbol Variable { get; private set; }
        public BoundExpression LowerBound { get; private set; }
        public BoundExpression UpperBound { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

        public BoundForStatement(VariableSymbol variable, BoundExpression lowerBound, BoundExpression upperBound, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel) : base(body, breakLabel, continueLabel)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }
    }
}