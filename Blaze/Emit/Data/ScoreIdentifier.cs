using Blaze.Emit.Nodes;
using Blaze.Symbols;

namespace Blaze.Emit.Data
{
    public class ScoreIdentifier : DataIdentifier, ScoreboardPlayersCommand.IScoreboardPlayersSubCommandClause
    {
        public override DataLocation Location => DataLocation.Scoreboard;
        public string Selector { get; }
        public string Objective { get; }

        public override string Text => $"{Selector} {Objective}";

        public ScoreIdentifier(string selector, string objective)
        {
            Selector = selector;
            Objective = objective;
        }
    }
}
