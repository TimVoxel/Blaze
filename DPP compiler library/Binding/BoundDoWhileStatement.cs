namespace DPP_Compiler.Binding
{
    internal sealed class BoundDoWhileStatement : BoundStatement
    {
        public BoundStatement Body { get; private set; }
        public BoundExpression Condition { get; private set; }
        
        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;

        public BoundDoWhileStatement(BoundStatement body, BoundExpression condition)
        {
            Condition = condition;
            Body = body;
        }
    }
}