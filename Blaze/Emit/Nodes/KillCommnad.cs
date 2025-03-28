namespace Blaze.Emit.Nodes
{
    public class KillCommnad : CommandNode
    {
        public string Selector { get; }

        public override string Keyword => "kill";
        public override EmittionNodeKind Kind => EmittionNodeKind.KillCommand;

        public override string Text => $"{Keyword} {Selector}";

        public KillCommnad(string selector)
        {
            Selector = selector;
        }
    }
}
