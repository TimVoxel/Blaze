namespace Blaze.Emit.Nodes
{
    public class DifficultyCommand : CommandNode
    {
        public string? Value { get; }

        public override EmittionNodeKind Kind => EmittionNodeKind.DifficultyCommand;
        public override string Keyword => "difficulty";

        public override string Text =>
            Value != null
                ? $"{Keyword} {Value}"
                : Keyword;

        public DifficultyCommand(string? value)
        {
            Value = value;
        }
    }
}
