using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes.Execute
{
    public class AnchoredExecuteSubCommand : ExecuteSubCommand
    {
        public FacingAnchor Anchor { get; }

        public override string Text => $"anchored {Anchor.ToString().ToLower()}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.Anchored;

        public AnchoredExecuteSubCommand(FacingAnchor anchor)
        {
            Anchor = anchor;
        }
    }

}
