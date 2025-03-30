using Blaze.Emit.Nodes;

namespace Blaze.Emit.Data
{
    public class Coordinates2 : IRotationClause
    {
        public string X { get; }
        public string Z { get; }

        public string Text => $"{X} {Z}";

        public Coordinates2(string x, string z)
        {
            X = x;
            Z = z;
        }

        public Coordinates3 WithY(string y) => new Coordinates3(X, y, Z);
    }
}
