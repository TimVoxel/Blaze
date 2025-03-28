using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes
{
    public class TeleportCommand : CommandNode
    {
        public override string Keyword => "teleport";
        public override EmittionNodeKind Kind => EmittionNodeKind.TeleportCommand;
        
        public string TargetSelector { get; }

        public override string Text => $"{Keyword} {TargetSelector}";

        public TeleportCommand(string targetSelector)
        {
            TargetSelector = targetSelector;
        }

        public static TeleportToLocationCommand ToLocation(string targetSelector, string x, string y, string z) => new TeleportToLocationCommand(targetSelector, new Coordinates3(x, y, z)); 
    }

    public class TeleportToEntityCommand : TeleportCommand
    {
        public string DestinationEntitySelector { get; }

        public override string Text => $"{Keyword} {TargetSelector} {DestinationEntitySelector}";

        public TeleportToEntityCommand(string targetSelector, string destinationEntitySelector) : base(destinationEntitySelector)
        {
            DestinationEntitySelector = destinationEntitySelector;
        }
    }

    public class TeleportToLocationCommand : TeleportCommand
    {
        public interface ITeleportRotationClause
        {
            public string Text { get; }
        }

        public class FacingLocationClause : ITeleportRotationClause
        {
            public Coordinates3 Location { get; }

            public string Text => $"facing {Location.Text}";

            public FacingLocationClause(Coordinates3 location)
            {
                Location = location;
            }
        }

        public class FacingEntityClause : ITeleportRotationClause
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

        public Coordinates3 Location { get; }
        public ITeleportRotationClause? RotationClause { get; }

        public override string Text =>
            RotationClause != null
                ? $"{Keyword} {TargetSelector} {Location.Text} {RotationClause.Text}"
                : $"{Keyword} {TargetSelector} {Location.Text}";

        public TeleportToLocationCommand(string selector, Coordinates3 location, ITeleportRotationClause? rotationClause = null) : base(selector)
        {
            Location = location;
            RotationClause = rotationClause;
        }
    }
}
