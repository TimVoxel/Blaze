namespace DPP_Compiler.Binding
{
    internal sealed class BoundReturnStatement : BoundStatement
    {
        public BoundExpression? Expression { get; private set; }
        public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;

        public BoundReturnStatement(BoundExpression? expression)
        {
            Expression = expression;
        }
    }
}