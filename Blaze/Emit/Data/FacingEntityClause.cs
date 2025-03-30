using Blaze.Emit.Nodes;

namespace Blaze.Emit.Data
{
    public class FacingEntityClause : IRotationClause
    {
        public string Selector { get; }
        public FacingAnchor? Anchor { get; }

        public string Text =>
            Anchor != null
                ? $"facing entity {Selector} {Anchor?.ToString().ToLower()}"
                : $"facing entity {Selector}";

        public FacingEntityClause(string selector, FacingAnchor? anchor)
        {
            Selector = selector;
            Anchor = anchor;
        }
    }
}
