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
        ForceloadCommand,
        GameruleCommand,
        ScoreboardCommand,
        SummonCommand,
        TeleportCommand,
        TellrawCommand,
        WeatherCommand,

        MacroCommand,
        TextBlock,

        //Temporary
        TextCommand,
    }
}
