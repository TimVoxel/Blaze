namespace Blaze.Emit.Nodes.Execute
{
    public class InExecuteSubCommand : ExecuteSubCommand
    {
        public string World { get; }

        public override string Text => $"in {World}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.In;

        public InExecuteSubCommand(string world)
        {
            World = world;
        }
    }

}
