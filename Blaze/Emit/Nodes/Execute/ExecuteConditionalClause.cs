using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes.Execute
{
    public abstract class ExecuteConditionalClause
    {
        public abstract string Text { get; }
    }

    public class IfBiomeClause : ExecuteConditionalClause
    {
        public Coordinates3 Position { get; }
        public string Biome { get; }

        public override string Text => $"biome {Position.Text} {Biome}";
       
        public IfBiomeClause(Coordinates3 position, string biome)
        {
            Position = position;
            Biome = biome;
        }
    }

    public class IfBlockClause : ExecuteConditionalClause
    {
        public Coordinates3 Position { get; }
        public string Block { get; }

        public override string Text => $"block {Position.Text} {Block}";

        public IfBlockClause(Coordinates3 position, string block)
        {
            Position = position;
            Block = block;
        }
    }

    public class IfBlocksClause : ExecuteConditionalClause
    {
        public enum MatchMode
        {
            All,
            Masked
        }

        public Coordinates3 FirstStart { get; }
        public Coordinates3 FirstEnd { get; }
        public Coordinates3 SecondStart { get; }
        public MatchMode Mode { get; }

        public override string Text => $"blocks {FirstStart.Text} {FirstEnd.Text} {SecondStart.Text} {Mode.ToString().ToLower()}";

        public IfBlocksClause(Coordinates3 firstStart, Coordinates3 firstEnd, Coordinates3 secondStart, MatchMode mode)
        {
            FirstStart = firstStart;
            FirstEnd = firstEnd;
            SecondStart = secondStart;
            Mode = mode;
        }
    }

    public class IfDataClause : ExecuteConditionalClause
    {
        public ObjectPathIdentifier PathIdentifier { get; }

        public override string Text => $"data {PathIdentifier.Text}";

        public IfDataClause(ObjectPathIdentifier pathIdentifier)
        {
            PathIdentifier = pathIdentifier;
        }
    }

    public class IfDimensionClause : ExecuteConditionalClause
    {
        public string Dimension { get; }

        public override string Text => $"dimension {Dimension}";

        public IfDimensionClause(string dimension)
        {
            Dimension = dimension;
        }
    }

    public class IfEntityClause : ExecuteConditionalClause
    {
        public string Selector { get; }

        public override string Text => $"entity {Selector}";

        public IfEntityClause(string selector)
        {
            Selector = selector;
        }
    }

    public class IfFunctionClause : ExecuteConditionalClause
    {
        public string FunctionCallName { get; }

        public override string Text => $"function {FunctionCallName}";

        public IfFunctionClause(string functionCallName)
        {
            FunctionCallName = functionCallName;
        }
    }

    public class IfItemsBlockClause : ExecuteConditionalClause
    {
        public Coordinates3 Position { get; }
        public string Slots { get; }
        public string ItemPredicate { get; }

        public override string Text => $"items block {Position.Text} {Slots} {ItemPredicate}";
        
        public IfItemsBlockClause(Coordinates3 position, string slots, string itemPredicate)
        {
            Position = position;
            Slots = slots;
            ItemPredicate = itemPredicate;
        }
    }

    public class IfItemsEntityClause : ExecuteConditionalClause
    {
        public string Selector { get; }
        public string Slots { get; }
        public string ItemPredicate { get; }

        public override string Text => $"items entity {Selector} {Slots} {ItemPredicate}";

        public IfItemsEntityClause(string selector, string slots, string itemPredicate)
        {
            Selector = selector;
            Slots = slots;
            ItemPredicate = itemPredicate;
        }
    }

    public class IfLoadedClause : ExecuteConditionalClause
    {
        public Coordinates3 Position { get; }

        public override string Text => $"loaded {Position.Text}";

        public IfLoadedClause(Coordinates3 position)
        {
            Position = position;
        }
    }

    public class IfPredicateClause : ExecuteConditionalClause
    {
        public string PredicateName { get; }

        public override string Text => $"predicate {PredicateName}";

        public IfPredicateClause(string predicateName)
        {
            PredicateName = predicateName;
        }
    }

    public class IfScoreClause : ExecuteConditionalClause
    {
        public enum ComparisonType
        {
            Greater,
            GreaterOrEquals, 
            Less,
            LessOrEquals,
            Equals
        }

        public ScoreIdentifier Left { get; }
        public ComparisonType Comparison { get; }
        public ScoreIdentifier Right { get; }

        public override string Text => $"score {Left.Text} {Comparison.GetSign()} {Right.Text}";

        public IfScoreClause(ScoreIdentifier left, ComparisonType comparison, ScoreIdentifier right)
        {
            Left = left;
            Comparison = comparison;
            Right = right;
        }
    }

    public class IfScoreMatchesClause : ExecuteConditionalClause
    {
        public ScoreIdentifier Identifier { get; }
        public string? LowerBound { get; }
        public string? UpperBound { get; }

        public override string Text => $"score {Identifier.Text} matches {EmittionFacts.GetRangeString(LowerBound, UpperBound)}";

        public IfScoreMatchesClause(ScoreIdentifier identifier, string? lowerBound, string? upperBound)
        {
            Identifier = identifier;
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }
    }
}
