namespace DPP_Compiler.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public BoundLabel Label { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

        public BoundLabelStatement(BoundLabel label)
        {
            Label = label;
        }

        public override IEnumerable<BoundNode> GetChildren() => Enumerable.Empty<BoundNode>();
    }
}
