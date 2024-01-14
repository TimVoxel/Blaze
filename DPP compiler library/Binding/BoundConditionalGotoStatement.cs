namespace DPP_Compiler.Binding
{
    internal sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public BoundLabel Label { get; private set; }
        public BoundExpression Condition { get; private set; }
        public bool JumpIfFalse { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

        public BoundConditionalGotoStatement(BoundLabel label, BoundExpression condition, bool jumpIfFalse = false)
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
