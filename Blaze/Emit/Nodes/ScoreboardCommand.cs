using Blaze.Binding;
using static Blaze.Emit.Nodes.ScoreboardObjectivesCommand;
namespace Blaze.Emit.Nodes
{
    public abstract class ScoreboardCommand : CommandNode
    {
        public class ScoreIdentifier
        {
            public string Selector { get; }
            public string Objective { get; }

            public string Text => $"{Selector} {Objective}";

            public ScoreIdentifier(string selector, string objective)
            {
                Selector = selector;
                Objective = objective;
            }

        }
        public override EmittionNodeKind Kind => EmittionNodeKind.ScoreboardCommand;

        protected ScoreboardCommand() : base("scoreboard") { }

        internal static ScoreboardObjectivesCommand ListObjectives() => new ScoreboardObjectivesCommand(string.Empty, SubAction.List);
        internal static ScoreboardObjectivesCommand AddObjective(string objective, string criteria) => new ScoreboardObjectivesCommand(objective, SubAction.Add, criteria: criteria);
        internal static ScoreboardObjectivesCommand RemoveObjective(string objective) => new ScoreboardObjectivesCommand(objective, SubAction.Remove);
        internal static ScoreboardObjectivesCommand SetDisplay(string objective, string displaySlot) => new ScoreboardObjectivesCommand(objective, SubAction.SetDisplay, displaySlot: displaySlot);
        internal static ScoreboardObjectivesCommand ModifyObjective(string objective, string modifiedProperty, object modifyValue) => new ScoreboardObjectivesCommand(objective, SubAction.Modify, modifiedProperty: modifiedProperty, modifyValue: modifyValue);


        internal static ScoreboardPlayersCommand GetScore(string selector, string objective, string? multiplier)
           => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Get, new ScoreboardPlayersCommand.ScoreIdentifierClause(selector, objective), multiplier);

        internal static ScoreboardPlayersCommand ScoreOperation(string leftSelector, string leftObjective, ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause.PlayersOperation operation, string rightSelector, string rightObjective)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Operations, 
                new ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause(leftSelector, leftObjective, operation, rightSelector, rightObjective),
                null);

        internal static ScoreboardPlayersCommand ScoreAdd(string selector, string objective, string value)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Add, new ScoreboardPlayersCommand.ScoreIdentifierClause(selector, objective), value);

        internal static ScoreboardPlayersCommand SetScore(string selector, string objective, string value)
           => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Set, new ScoreboardPlayersCommand.ScoreIdentifierClause(selector, objective), value);

        internal static ScoreboardPlayersCommand ScoreSubtract(string selector, string objective, string value)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Remove, new ScoreboardPlayersCommand.ScoreIdentifierClause(selector, objective), value);

        internal static ScoreboardPlayersCommand ScoreReset(string selector, string objective)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Reset, new ScoreboardPlayersCommand.ScoreIdentifierClause(selector, objective), null);

        internal static ScoreboardPlayersCommand TriggerEnable(string selector, string objective)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Enable, new ScoreboardPlayersCommand.ScoreIdentifierClause(selector, objective), null);

        internal static ScoreboardPlayersCommand ScoreList(string? selector)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.List, new ScoreboardPlayersCommand.ListTarget(selector), null);

        internal static ScoreboardPlayersCommand ScoreDisplayName(string selector, string objective, string name)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Display, new ScoreboardPlayersCommand.DisplayNameClause(selector, objective, name), null);
        internal static ScoreboardPlayersCommand ScoreNumberFormat(string selector, string objective, ScoreboardPlayersCommand.DisplayNumberFormatClause.NumberFormat format, string? style)
            => new ScoreboardPlayersCommand(ScoreboardPlayersCommand.SubAction.Display, new ScoreboardPlayersCommand.DisplayNumberFormatClause(selector, objective, format, style), null);

    }
}
