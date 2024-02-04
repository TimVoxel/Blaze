namespace Blaze.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public BoundLabel Label { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

        public BoundLabelStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}