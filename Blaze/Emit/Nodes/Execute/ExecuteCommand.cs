using System.Collections.Immutable;
using System.Text;

namespace Blaze.Emit.Nodes.Execute
{
    public class ExecuteCommand : CommandNode
    {
        public ImmutableArray<ExecuteSubCommand> SubCommands { get; }
        public CommandNode RunCommand { get; }

        public override string Keyword => "execute";
        public override EmittionNodeKind Kind => EmittionNodeKind.ExecuteCommand;

        public override string Text
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append(Keyword);

                foreach (var subCommand in SubCommands)
                    builder.Append($" {subCommand.Text}");

                builder.Append($" run {RunCommand.Text}");
                return builder.ToString();
            }
        }

        public ExecuteCommand(ImmutableArray<ExecuteSubCommand> subCommands, CommandNode runCommand)
        {
            SubCommands = subCommands;
            RunCommand = runCommand;
        }

        public static ExecuteCommand Run(CommandNode runCommand, params ExecuteSubCommand[] subCommands) => new ExecuteCommand(subCommands.ToImmutableArray(), runCommand);
    }
}
