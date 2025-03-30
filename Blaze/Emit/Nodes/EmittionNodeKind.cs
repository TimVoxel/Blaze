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
        ExecuteCommand,
        FunctionCommand,
        ForceloadCommand,
        GameruleCommand,
        KillCommand,
        ReturnCommand,
        ScoreboardCommand,
        SummonCommand,
        TagCommand,
        TeleportCommand,
        TellrawCommand,
        WeatherCommand,

        MacroCommand,
        TextBlock,

        //Temporary
        TextCommand,
    }
}
