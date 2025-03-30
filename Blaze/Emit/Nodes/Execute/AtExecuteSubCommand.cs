namespace Blaze.Emit.Nodes.Execute
{
    public class AtExecuteSubCommand : ExecuteSubCommand
    {
        public string Selector { get; }

        public override string Text => $"at {Selector}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.At;

        public AtExecuteSubCommand(string selector)
        {
            Selector = selector;
        }
    }

}
