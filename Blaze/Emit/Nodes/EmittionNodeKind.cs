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

        DataCommand,
        DatapackCommand,
        DifficultyCommand,
        FunctionCommand,
        GameruleCommand,
        ScoreboardCommand,
        TellrawCommand,
        WeatherCommand,
        
        TextBlock,

        //Temporary
        TextCommand,
    }
}
