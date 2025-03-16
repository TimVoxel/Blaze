namespace Blaze.Emit.Nodes
{
    public enum EmittionNodeKind
    {
        Datapack,
        Namespace,
        MinecraftFunction,

        EmptyTrivia,
        LineBreakTrivia,
        CommentTrivia,

        ScoreboardCommand,
        FunctionCommand,

        TextBlock,

        //Temporary
        TextCommand,
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
