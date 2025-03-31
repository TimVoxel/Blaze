using Blaze.Emit.Data;
using static Blaze.Emit.Nodes.ScoreboardObjectivesCommand;

namespace Blaze.Emit.Nodes
{
    public abstract partial class ScoreboardCommand : CommandNode
    {
        public override EmittionNodeKind Kind => EmittionNodeKind.ScoreboardCommand;
        public override string Keyword => "scoreboard";

        protected ScoreboardCommand() : base() { }

        internal static ScoreboardObjectivesCommand ListObjectives() => new ScoreboardObjectivesCommand(string.Empty, SubAction.List);
        internal static ScoreboardObjectivesCommand AddObjective(string objective, string criteria) => new ScoreboardObjectivesCommand(objective, SubAction.Add, criteria: criteria);
        internal static ScoreboardObjectivesCommand RemoveObjective(string objective) => new ScoreboardObjectivesCommand(objective, SubAction.Remove);
        internal static ScoreboardObjectivesCommand SetDisplay(string objective, string displaySlot) => new ScoreboardObjectivesCommand(objective, SubAction.SetDisplay, displaySlot: displaySlot);
        internal static ScoreboardObjectivesCommand ModifyObjective(string objective, string modifiedProperty, object modifyValue) => new ScoreboardObjectivesCommand(objective, SubAction.Modify, modifiedProperty: modifiedProperty, modifyValue: modifyValue);

        internal static ScoreboardPlayersCommand GetScore(ScoreIdentifier identifier, string? multiplier)
           => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Get, identifier, multiplier);

        internal static ScoreboardPlayersCommand ScoreOperation(ScoreIdentifier left, ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause.PlayersOperation operation, ScoreIdentifier right)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Operation, 
                new ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause(left, operation, right),
                null);

        internal static ScoreboardPlayersCommand ScoreAdd(ScoreIdentifier identifier, string value)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Add, identifier, value);

        internal static ScoreboardPlayersCommand SetScore(ScoreIdentifier identifier, string value)
           => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Set, identifier, value);

        internal static ScoreboardPlayersCommand ScoreSubtract(ScoreIdentifier identifier, string value)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Remove, identifier, value);

        internal static ScoreboardPlayersCommand ScoreReset(ScoreIdentifier identifier)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Reset, identifier, null);

        internal static ScoreboardPlayersCommand TriggerEnable(ScoreIdentifier identifier)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Enable, identifier, null);

        internal static ScoreboardPlayersCommand ScoreList(string? selector)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.List, new ScoreboardPlayersCommand.ListTarget(selector), null);

        internal static ScoreboardPlayersCommand ScoreDisplayName(string selector, string objective, string name)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Display, new ScoreboardPlayersCommand.DisplayNameClause(selector, objective, name), null);
        internal static ScoreboardPlayersCommand ScoreNumberFormat(string selector, string objective, ScoreboardPlayersCommand.DisplayNumberFormatClause.NumberFormat format, string? style)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Display, new ScoreboardPlayersCommand.DisplayNumberFormatClause(selector, objective, format, style), null);

    }
}
