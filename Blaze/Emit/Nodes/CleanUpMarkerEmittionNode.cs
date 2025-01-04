namespace Blaze.Emit.Nodes
{
    public sealed class CleanUpMarkerEmittionNode : TextEmittionNode
    {
        public override EmittionNodeKind Kind => EmittionNodeKind.CleanUpMarker;
        public override bool IsCleanUp => false;
        public override string Text => string.Empty;
        public override bool IsSingleLine => false;
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
