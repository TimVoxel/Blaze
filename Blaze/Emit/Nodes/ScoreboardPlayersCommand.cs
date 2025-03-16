namespace Blaze.Emit.Nodes
{
    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        public abstract class ScoreboardPlayersSubCommandClause
        {
            public abstract string Text { get; }
        }

        public class ListTarget : ScoreboardPlayersSubCommandClause
        {
            public override string Text { get; }
            
            public ListTarget(string? target)
            {
                Text = target ?? string.Empty;
            }
        }

        public class ScoreIdentifierClause : ScoreboardPlayersSubCommandClause
        {
            public ScoreIdentifier Score;

            public override string Text => Score.Text;

            public ScoreIdentifierClause(string selector, string objective)
            {
                Score = new ScoreIdentifier(selector, objective);
            }
        }

        public class ScoreboardPlayersOperationsClause : ScoreboardPlayersSubCommandClause
        {
            public enum PlayersOperation 
            {
                Assignment,
                Addition,
                Subtraction,
                Multiplication,
                Division,
                Mod,
                Min,
                Max,
                Swap
            }

            public ScoreIdentifier Left { get; }
            public PlayersOperation Operation { get; }
            public ScoreIdentifier Right { get; }
            
            public override string Text => $"{Left.Text} {EmittionFacts.GetSignText(Operation)} {Right.Text}";

            public ScoreboardPlayersOperationsClause(string leftSelector, string leftObjective, PlayersOperation operation, string rightSelector, string rightObjective)
            {
                Left = new ScoreIdentifier(leftSelector, leftObjective);
                Operation = operation;
                Right = new ScoreIdentifier(rightSelector, rightObjective);
            }
        }

        public class DisplayNameClause : ScoreboardPlayersSubCommandClause
        {
            public ScoreIdentifier Score { get; }
            public string Name { get; }

            public override string Text => $"{Score.Text} {Name}";

            public DisplayNameClause(string selector, string objective, string name)
            {
                Score = new ScoreIdentifier(selector, objective);
                Name = name;
            }
        }

        public class DisplayNumberFormatClause : ScoreboardPlayersSubCommandClause
        {
            public enum NumberFormat
            {
                Fixed,
                Blank,
                Styled
            }
            public ScoreIdentifier Score { get; }
            public NumberFormat Format { get; }
            public string? Style { get; }

            public override string Text =>
                Format == NumberFormat.Styled
                    ? $"{Score.Text} {Format.ToString().ToLower()} {Style}"
                    : $"{Score.Text} {Format.ToString().ToLower()}";

            public DisplayNumberFormatClause(string selector, string objective, NumberFormat format, string? style)
            {
                Score = new ScoreIdentifier(selector, objective);
                Format = format;
                Style = style;
            }
        }

        public enum SubAction
        {
            Add,
            Set,
            Get,
            Remove,
            Operations,
            Reset,
            Enable,
            List,
            Display
        }

        public SubAction Action { get; }
        public ScoreboardPlayersSubCommandClause SubClause { get; }
        public string? Value { get; }

        public override bool IsCleanUp => Action == SubAction.Reset;
        public override string Text =>
            Value == null
                ? $"{Keyword} players {Action.ToString().ToLower()} {SubClause.Text}"
                : $"{Keyword} players {Action.ToString().ToLower()} {SubClause.Text} {Value}";
        
        internal ScoreboardPlayersCommand(SubAction action, ScoreboardPlayersSubCommandClause subClause, string? value)
        {
            Action = action;
            SubClause = subClause;
            Value = value;
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
