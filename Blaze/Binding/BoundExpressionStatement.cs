namespace Blaze.Binding
{
    internal sealed class BoundExpressionStatement : BoundStatement
    {
        public BoundExpression Expression { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;

        public BoundExpressionStatement(BoundExpression expression)
        {
            Expression = expression;
        }
    }
}