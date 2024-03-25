namespace Blaze.Binding
{
    internal sealed class BoundContinueStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ContinueStatement;

        public BoundContinueStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}