using Blaze.Emit.Nodes;

namespace Blaze.Emit.Data
{
    public class FacingLocationClause : IRotationClause
    {
        public Coordinates3 Location { get; }

        public string Text => $"facing {Location.Text}";

        public FacingLocationClause(Coordinates3 location)
        {
            Location = location;
        }
    }
}
