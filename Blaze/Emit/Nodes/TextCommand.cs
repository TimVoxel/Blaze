namespace Blaze.Emit.Nodes
{
    public class TextCommand : CommandNode
    {
        public override string Text { get; }
        public override EmittionNodeKind Kind => EmittionNodeKind.TextCommand;
        public override string Keyword => Text.Split().First();

        public TextCommand(string text)
        {
            Text = text;
        }
    }
}
