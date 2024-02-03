namespace DPP_Compiler.Binding
{
    internal sealed class BoundWhileStatement : BoundStatement
    {
        public BoundExpression Condition { get; private set; }
        public BoundStatement Body { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;

        public BoundWhileStatement(BoundExpression condition, BoundStatement body)
        {
            Condition = condition;
            Body = body;
        }
    }
}