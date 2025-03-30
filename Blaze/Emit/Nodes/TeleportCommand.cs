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
        public Coordinates3 Location { get; }
        public IRotationClause? RotationClause { get; }

        public override string Text =>
            RotationClause != null
                ? $"{Keyword} {TargetSelector} {Location.Text} {RotationClause.Text}"
                : $"{Keyword} {TargetSelector} {Location.Text}";

        public TeleportToLocationCommand(string selector, Coordinates3 location, IRotationClause? rotationClause = null) : base(selector)
        {
            Location = location;
            RotationClause = rotationClause;
        }
    }
}
