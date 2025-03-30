namespace Blaze.Emit.Nodes.Execute
{
    public class AsExecuteSubCommand : ExecuteSubCommand
    {
        public string Selector { get; }
        public override string Text => $"as {Selector}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.As;

        public AsExecuteSubCommand(string selector)
        {
            Selector = selector;
        }
    }

}
