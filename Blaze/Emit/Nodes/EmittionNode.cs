using System.Diagnostics;
using static Blaze.Emit.Nodes.ScoreboardObjectivesCommand;

namespace Blaze.Emit.Nodes
{
    public abstract class EmittionNode
    {
        public abstract EmittionNodeKind Kind { get; }

        public EmittionNode() { }
    }

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

    public class ScoreboardObjectivesCommand : ScoreboardCommand
    {
        public enum SubAction
        {
            Add,
            Remove,
            List,
            Modify,
            SetDisplay
        }

        public string Objective { get; }
        public SubAction Action { get; }
        public string? DisplayName { get; }
        public string? Criteria { get; }
        public string? DisplaySlot { get; }
        public string? ModifiedProperty { get; }
        public object? ModifyValue { get; }

        public override bool IsCleanUp => false;

        public override string Text
        {
            get
            {
                var baseText = $"{Keyword} objectives {Action.ToString().ToLower()}";

                switch (Action)
                {
                    case SubAction.List:
                        return baseText;
                    case SubAction.Add:
                        Debug.Assert(Criteria != null);
                        var displayNameSuffix = DisplayName == null ? string.Empty : $" {DisplayName}";
                        return $"{baseText} {Objective} {Criteria}{displayNameSuffix}";
                    case SubAction.Remove:
                        return $"{baseText} {Objective}";
                    case SubAction.SetDisplay:
                        Debug.Assert(DisplaySlot != null);
                        return $"{baseText} {DisplaySlot} {Objective}";
                    case SubAction.Modify:
                        Debug.Assert(ModifiedProperty != null);
                        Debug.Assert(ModifyValue != null);
                        return $"{baseText} {Objective} {ModifiedProperty} {ModifyValue}";
                }

                if (Action == SubAction.List)
                    return $"{Keyword} objectives {Action.ToString().ToLower()}";
                else
                    return $"{Keyword} objectives {Action.ToString().ToLower()} {Objective}";
            }
        }

        internal ScoreboardObjectivesCommand(string objective, SubAction action, string? criteria = null, string? displayName = null, string? displaySlot = null, string? modifiedProperty = null, object? modifyValue = null) : base()
        {
            Objective = objective;
            Action = action;
            DisplayName = displayName;
            Criteria = criteria;
            DisplaySlot = displaySlot;
            ModifiedProperty = modifiedProperty;
            ModifyValue = modifyValue;
        }
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
