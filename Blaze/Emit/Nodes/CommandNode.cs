namespace Blaze.Emit.Nodes
{
    public abstract class CommandNode : TextEmittionNode
    {
        public override bool IsSingleLine => true;
        public string Keyword { get; }

        public CommandNode(string keyword) : base()
        {
            Keyword = keyword;
        }
    }

    public class TextCommand : CommandNode
    {
        //This class is a placeholder and should be replaced by specific command classes

        public override string Text { get; }
        public override EmittionNodeKind Kind => EmittionNodeKind.TextCommand;
        public override bool IsCleanUp { get; }
        
        public TextCommand(string text, bool isCleanUp) : base(text.Split().First())
        {
            Text = text;
            IsCleanUp = isCleanUp;
        }
    }

    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
