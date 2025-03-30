using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes.Execute
{
    public class RotatedExecuteSubCommand : ExecuteSubCommand
    {
        public Coordinates2 Rotation { get; }

        public override string Text => $"rotated {Rotation.Text}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.Rotated;

        public RotatedExecuteSubCommand(Coordinates2 rotation)
        {
            Rotation = rotation;
        }
    }

    public class RotatedAsExecuteSubCommand : ExecuteSubCommand
    {
        public string Selector { get; }

        public override string Text => $"rotated as {Selector}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.RotatedAs;

        public RotatedAsExecuteSubCommand(string selector)
        {
            Selector = selector;
        }
    }

}
