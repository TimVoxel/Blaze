namespace Blaze.Binding
{
    internal sealed class BoundBreakStatement : BoundStatement
    {
        public BoundLabel Label { get; }
        public override BoundNodeKind Kind => BoundNodeKind.BreakStatement;

        public BoundBreakStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}