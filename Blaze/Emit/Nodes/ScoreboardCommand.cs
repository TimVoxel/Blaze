using static Blaze.Emit.Nodes.ScoreboardObjectivesCommand;

namespace Blaze.Emit.Nodes
{
    public abstract class ScoreboardCommand : CommandNode
    {
        public override EmittionNodeKind Kind => EmittionNodeKind.ScoreboardCommand;

        protected ScoreboardCommand() : base("scoreboard") { }

        public static ScoreboardObjectivesCommand ListObjectives() => new ScoreboardObjectivesCommand(string.Empty, SubAction.List);
        public static ScoreboardObjectivesCommand AddObjective(string objective, string criteria) => new ScoreboardObjectivesCommand(objective, SubAction.Add, criteria: criteria);
        public static ScoreboardObjectivesCommand RemoveObjective(string objective) => new ScoreboardObjectivesCommand(objective, SubAction.Remove);
        public static ScoreboardObjectivesCommand SetDisplay(string objective, string displaySlot) => new ScoreboardObjectivesCommand(objective, SubAction.SetDisplay, displaySlot: displaySlot);
        public static ScoreboardObjectivesCommand ModifyObjective(string objective, string modifiedProperty, object modifyValue) => new ScoreboardObjectivesCommand(objective, SubAction.Modify, modifiedProperty: modifiedProperty, modifyValue: modifyValue);
    }

    /*

    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        enum SubAction 
        {
            Add,
            Remove,
            Reset
            
        }
           
        internal ScoreboardPlayersCommand()
        {

        }
    }*/
}
