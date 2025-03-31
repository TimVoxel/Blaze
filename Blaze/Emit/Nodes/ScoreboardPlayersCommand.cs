using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes
{
    public class ScoreboardPlayersCommand : ScoreboardCommand
    {
        public interface IScoreboardPlayersSubCommandClause
        {
            public string Text { get; }
        }

        public class ListTarget : IScoreboardPlayersSubCommandClause
        {
            public string Text { get; }

            public ListTarget(string? target)
            {
                Text = target ?? string.Empty;
            }
        }

        public class ScoreboardPlayersOperationsClause : IScoreboardPlayersSubCommandClause
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

            public string Text => $"{Left.Text} {EmittionFacts.GetSign(Operation)} {Right.Text}";

            public ScoreboardPlayersOperationsClause(ScoreIdentifier left, PlayersOperation operation, ScoreIdentifier right)
            {
                Left = left;
                Operation = operation;
                Right = right;
            }
        }

        public class DisplayNameClause : IScoreboardPlayersSubCommandClause
        {
            public ScoreIdentifier Score { get; }
            public string Name { get; }

            public string Text => $"{Score.Text} {Name}";

            public DisplayNameClause(string selector, string objective, string name)
            {
                Score = new ScoreIdentifier(selector, objective);
                Name = name;
            }
        }

        public class DisplayNumberFormatClause : IScoreboardPlayersSubCommandClause
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

            public string Text =>
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
            Operation,
            Reset,
            Enable,
            List,
            Display
        }

        public SubAction Action { get; }
        public IScoreboardPlayersSubCommandClause SubClause { get; }
        public string? Value { get; }

        public override string Text =>
            Value == null
                ? $"{Keyword} players {Action.ToString().ToLower()} {SubClause.Text}"
                : $"{Keyword} players {Action.ToString().ToLower()} {SubClause.Text} {Value}";

        internal ScoreboardPlayersCommand(SubAction action, IScoreboardPlayersSubCommandClause subClause, string? value)
        {
            Action = action;
            SubClause = subClause;
            Value = value;
        }
    }
}
