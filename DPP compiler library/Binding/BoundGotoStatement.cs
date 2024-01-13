namespace DPP_Compiler.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public LabelSymbol Label { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.GoToStatement;

        public BoundGotoStatement(LabelSymbol label)
        {
            Label = label;
        }

        public override IEnumerable<BoundNode> GetChildren() => Enumerable.Empty<BoundNode>();
    }
}
