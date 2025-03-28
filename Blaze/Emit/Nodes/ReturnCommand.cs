namespace Blaze.Emit.Nodes
{
    public abstract class ReturnCommand : CommandNode
    {
        public override string Keyword => "return";
        public override EmittionNodeKind Kind => EmittionNodeKind.ReturnCommand;

        public static ReturnValueCommand ReturnFail() => new ReturnValueCommand("fail");
    }

    public class ReturnValueCommand : ReturnCommand
    {
        public string Value { get; }

        public override string Text => $"{Keyword} {Value}";

        public ReturnValueCommand(string value)
        {
            Value = value;
        }
    }

    public class ReturnRunCommand : ReturnCommand
    {
        public CommandNode Command { get; }

        public override string Text => $"{Keyword} run {Command.Text}";

        public ReturnRunCommand(CommandNode command)
        {
            Command = command;
        }
    }
}
