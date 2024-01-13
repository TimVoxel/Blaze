namespace DPP_Compiler.Binding
{
    internal sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public LabelSymbol Label { get; private set; }
        public BoundExpression Condition { get; private set; }
        public bool JumpIfFalse { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

        public BoundConditionalGotoStatement(LabelSymbol label, BoundExpression condition, bool jumpIfFalse = false)
        {
            Label = label;
            Condition = condition;
            JumpIfFalse = jumpIfFalse;
        }

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Condition;
        }
    }
}
