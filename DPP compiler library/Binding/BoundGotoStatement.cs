namespace DPP_Compiler.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public BoundLabel Label { get; private set; }

        public override BoundNodeKind Kind => BoundNodeKind.GoToStatement;

        public BoundGotoStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}
