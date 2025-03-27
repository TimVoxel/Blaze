using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public class MacroCommand : CommandNode
    {
        public override string Keyword => Command.Keyword;
        public CommandNode Command { get; }
        public override EmittionNodeKind Kind => EmittionNodeKind.MacroCommand;

        public override string Text => $"${Command.Text}";

        public MacroCommand(CommandNode command)
        {
            Command = command;
        }
    }

    public abstract class DatapackCommand : CommandNode
    {
        public override EmittionNodeKind Kind => EmittionNodeKind.DatapackCommand;
        public override string Keyword => "datapack";
    }

    public class DatapackDisableCommand : DatapackCommand
    {
        public string Name { get; }

        public override string Text => $"{Keyword} disable {PackFullName}";
        public string PackFullName => $"\"file/{Name}\"";

        public DatapackDisableCommand(string name)
        {
            Name = name;
        }
    }

    public class DatapackEnableCommand : DatapackCommand
    {
        public enum EnableMode
        {
            First,
            Last,
            Before,
            After
        }

        public string PackName { get; }
        public EnableMode? Mode { get; }
        public string? OtherPackName { get; }
        public string PackFullName => $"\"file/{PackName}\"";
        public string OtherPackFullName => $"\"file/{OtherPackName}\"";

        public override string Text
        {
            get
            {
                if (Mode != null)
                {
                    if (Mode == EnableMode.First || Mode == EnableMode.Last)
                        return $"{Keyword} enable {PackFullName} {Mode?.ToString().ToLower()}";
                    else
                    {
                        Debug.Assert(OtherPackName != null);
                        return $"{Keyword} enable {PackFullName} {Mode?.ToString().ToLower()} {OtherPackFullName}";
                    }
                }
                else
                    return $"{Keyword} enable {PackFullName}";
            }
        }

        public DatapackEnableCommand(string name, EnableMode? mode = null, string? otherName = null)
        {
            PackName = name;
            Mode = mode;
            OtherPackName = otherName;
        }
    }

    public class DatapackListCommand : DatapackCommand
    {
        public enum ListFilter
        {
            Enabled,
            Available
        }

        public ListFilter? Filter { get; }

        public override string Text =>
            Filter != null
                ? $"{Keyword} list {Filter?.ToString().ToLower()}"
                : $"{Keyword} list";

        public DatapackListCommand(ListFilter filter)
        {
            Filter = filter;
        }
    }

}
