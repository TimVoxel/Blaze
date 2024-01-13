namespace DPP_Compiler.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public LabelSymbol Label { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

        public BoundLabelStatement(LabelSymbol label)
        {
            Label = label;
        }

        public override IEnumerable<BoundNode> GetChildren() => Enumerable.Empty<BoundNode>();
    }
}
