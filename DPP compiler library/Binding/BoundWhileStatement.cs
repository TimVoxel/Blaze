namespace DPP_Compiler.Binding
{
    internal sealed class BoundWhileStatement : BoundLoopStatement
    {
        public BoundExpression Condition { get; private set; }
        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

        public BoundWhileStatement(BoundExpression condition, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel) : base(body, breakLabel, continueLabel)
        {
            Condition = condition;
        }
    }
}