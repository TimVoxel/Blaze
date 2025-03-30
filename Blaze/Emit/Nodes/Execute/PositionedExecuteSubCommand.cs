using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes.Execute
{
    public class PositionedExecuteSubCommand : ExecuteSubCommand
    {
        public Coordinates3 Coords { get; }

        public override string Text => $"positioned {Coords.Text}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.Positioned;

        public PositionedExecuteSubCommand(Coordinates3 coordinates)
        {
            Coords = coordinates;
        }
    }

    public class PositionedAsExecuteSubCommand : ExecuteSubCommand
    {
        public string Selector { get; }

        public override string Text => $"positioned as {Selector}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.PositionedAs;

        public PositionedAsExecuteSubCommand(string selector)
        {
            Selector = selector;
        }
    }

    public class PositionedOverExecuteSubCommand : ExecuteSubCommand
    {
        public enum HeightMapType
        {
            MotionBlocking,
            MotionBlockNoLeaves,
            OceanFloor,
            WorldSurface
        }

        public HeightMapType HeightMap { get; }

        public override string Text => $"positioned over {EmittionFacts.GetSyntaxName(HeightMap)}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.PositionedOver;

        public PositionedOverExecuteSubCommand(HeightMapType heightMap)
        {
            HeightMap = heightMap;
        }
    }
}
