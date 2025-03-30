namespace Blaze.Emit.Nodes.Execute
{
    public class FacingExecuteSubCommand : ExecuteSubCommand
    {
        public IRotationClause RotationClause { get; }

        public override string Text => RotationClause.Text;
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.Facing;

        public FacingExecuteSubCommand(IRotationClause rotationClause)
        {
            RotationClause = rotationClause;
        }
    }

}
