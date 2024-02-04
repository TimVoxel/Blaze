namespace Blaze.Binding
{
    internal sealed class BoundDoWhileStatement : BoundLoopStatement
    {
        public BoundExpression Condition { get; private set; }
        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;

        public BoundDoWhileStatement(BoundStatement body, BoundExpression condition, BoundLabel breakLabel, BoundLabel continueLabel) : base(body, breakLabel, continueLabel)
        {
            Condition = condition;
        }
    }
}