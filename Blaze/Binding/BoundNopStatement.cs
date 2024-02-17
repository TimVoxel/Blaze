namespace Blaze.Binding
{
    internal sealed class BoundNopStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
    }
}