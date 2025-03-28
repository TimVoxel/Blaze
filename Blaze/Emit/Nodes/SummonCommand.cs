using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes
{
    public class SummonCommand : CommandNode
    {
        public string EntityType { get; }
        public Coordinates3 Location { get; }
        public string? Nbt { get; }

        public override string Keyword => "summon";
        public override EmittionNodeKind Kind => EmittionNodeKind.SummonCommand;
        public override string Text =>
            Nbt != null
                ? $"{Keyword} {EntityType} {Location.Text} {Nbt}"
                : $"{Keyword} {EntityType} {Location.Text}";

        public SummonCommand(string entityType, Coordinates3 location, string? nbt = null)
        {
            EntityType = entityType;
            Location = location;
            Nbt = nbt;
        }
    }
}
