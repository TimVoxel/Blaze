namespace Blaze.Emit.Nodes
{
    public enum EmittionNodeKind
    {
        Datapack,
        Namespace,
        MinecraftFunction,

        LineBreakTrivia,
        CommentTrivia,

        ScoreboardCommand,

        //Temporary
        TextCommand,

        //Markers
        CleanUpMarker
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
