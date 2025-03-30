namespace Blaze.Emit.Nodes.Execute
{
    public class AlignExecuteSubCommand : ExecuteSubCommand
    {
        public string Axis { get; }
        public override string Text => $"align {Axis}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.Align;

        public AlignExecuteSubCommand(string axis) 
        {
            Axis = axis;
        }
    }
}
