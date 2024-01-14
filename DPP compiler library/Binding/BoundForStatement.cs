using DPP_Compiler.Symbols;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundForStatement : BoundStatement
    {
        public VariableSymbol Variable { get; private set; }
        public BoundExpression LowerBound { get; private set; }
        public BoundExpression UpperBound { get; private set; }
        public BoundStatement Body { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

        public BoundForStatement(VariableSymbol variable, BoundExpression lowerBound, BoundExpression upperBound, BoundStatement body)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Body = body;
        }

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return LowerBound;
            yield return UpperBound;
            yield return Body;
        }
    }
}