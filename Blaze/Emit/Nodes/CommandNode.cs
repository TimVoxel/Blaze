using static System.Net.Mime.MediaTypeNames;

namespace Blaze.Emit.Nodes
{
    public abstract class CommandNode : TextEmittionNode
    {
        public override bool IsSingleLine => true;
        public abstract string Keyword { get; }
    }

    public class TextCommand : CommandNode
    {
        //This class is a placeholder and should be replaced by specific command classes

        public override string Text { get; }
        public override EmittionNodeKind Kind => EmittionNodeKind.TextCommand;
        public override bool IsCleanUp { get; }
        public override string Keyword => Text.Split().First();

        public TextCommand(string text, bool isCleanUp)
        {
            Text = text;
            IsCleanUp = isCleanUp;
        }
    }
}
