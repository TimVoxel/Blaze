namespace Blaze.Emit.Nodes
{
    public abstract class CommandNode : TextEmittionNode
    {
        public override bool IsSingleLine => true;
        public abstract string Keyword { get; }
    }

    public class TextCommand : CommandNode
    {
        public override string Text { get; }
        public override EmittionNodeKind Kind => EmittionNodeKind.TextCommand;
        public override string Keyword => Text.Split().First();

        public TextCommand(string text, bool isCleanUp)
        {
            Text = text;
        }
    }
}
