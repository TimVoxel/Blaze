namespace Blaze.Emit.Nodes.Execute
{
    public abstract class ExecuteSubCommand
    {
        public abstract string Text { get; }
        public abstract ExecuteSubCommandKind Kind { get; }
    }
}
