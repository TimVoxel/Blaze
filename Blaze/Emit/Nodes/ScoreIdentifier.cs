using Blaze.Symbols;

namespace Blaze.Emit.Nodes
{
    public class ScoreIdentifier : DataIdentifier
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
