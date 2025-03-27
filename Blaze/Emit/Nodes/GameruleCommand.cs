using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class GameruleCommand : CommandNode
    {
        public string GameruleName { get; }
        public string? Value { get; }

        public override EmittionNodeKind Kind => EmittionNodeKind.GameruleCommand;
        public override string Keyword => "gamerule";

        public override string Text =>
            Value != null
                ? $"{Keyword} {GameruleName} {Value}"
                : $"{Keyword} {GameruleName}";

        public GameruleCommand(string gameruleName, string? value)
        {
            GameruleName = gameruleName;
            Value = value;
        }
    }
}
