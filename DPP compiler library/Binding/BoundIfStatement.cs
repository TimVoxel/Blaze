namespace DPP_Compiler.Binding
{
    internal sealed class BoundIfStatement : BoundStatement
    {
        public BoundExpression Condition { get; private set; }
        public BoundStatement Body { get; private set; }
        public BoundStatement? ElseBody { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.IfStatement;

        public BoundIfStatement(BoundExpression condition, BoundStatement body, BoundStatement? elseBody)
        {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
        }

        public override IEnumerable<BoundNode> GetChildren()
        {
            yield return Condition;
            yield return Body;
            if (ElseBody != null)
                yield return ElseBody;
        }
    }
}