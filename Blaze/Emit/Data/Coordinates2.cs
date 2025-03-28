using Blaze.Emit.Nodes;

namespace Blaze.Emit.Data
{
    public class Coordinates2 : TeleportToLocationCommand.ITeleportRotationClause
    {
        public string X { get; }
        public string Z { get; }

        public string Text => $"{X} {Z}";

        public Coordinates2(string x, string z)
        {
            X = x;
            Z = z;
        }
    }
}
