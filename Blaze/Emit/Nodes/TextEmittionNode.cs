namespace Blaze.Emit.Nodes
{
    public abstract class TextEmittionNode : EmittionNode
    {
        public abstract string Text { get; }
        public abstract bool IsSingleLine { get; }
        public abstract bool IsCleanUp { get; }

        public TextEmittionNode() : base() { }
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
