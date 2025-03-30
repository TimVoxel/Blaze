using Blaze.Emit.Data;

namespace Blaze.Emit.Nodes.Execute
{
    public abstract class StoreExecuteSubCommand : ExecuteSubCommand
    {
        public enum StoreType
        {
            Byte,
            Double,
            Float,
            Int,
            Short,
            Long
        }

        public enum YieldType
        {
            Result,
            Success,
        }

        public YieldType Yield { get; }

        public StoreExecuteSubCommand(YieldType yield)
        {
            Yield = yield;
        }
    }

    public class StorePathExecuteSubCommand : StoreExecuteSubCommand
    {
        public ObjectPathIdentifier Identifier { get; }
        public StoreType ConvertType { get; }
        public string Scale { get; }

        public override string Text => $"store {Yield.ToString().ToLower()} {Identifier.Text} {ConvertType.ToString().ToLower()} {Scale}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.StorePath;

        public StorePathExecuteSubCommand(YieldType type, ObjectPathIdentifier identifier, StoreType convertType, string scale) : base(type)
        {
            Identifier = identifier;
            ConvertType = convertType;
            Scale = scale;
        }

    }

    public class StoreScoreExecuteSubCommand : StoreExecuteSubCommand
    {
        public ScoreIdentifier Identifier { get; }

        public override string Text => $"store {Yield.ToString().ToLower()} score {Identifier.Text}";
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.StoreScore;

        public StoreScoreExecuteSubCommand(YieldType yield, ScoreIdentifier identifier) : base(yield)
        {
            Identifier = identifier;
        }
    }

    public class StoreBossbarExecuteSubCommand : StoreExecuteSubCommand
    {
        public enum Property
        {
            Max,
            Value
        }

        public string BossbarName { get; }
        public Property StoreProperty { get; }
        public override ExecuteSubCommandKind Kind => ExecuteSubCommandKind.StoreBossbar;

        public override string Text => $"store {Yield.ToString().ToLower()} {BossbarName} {StoreProperty.ToString().ToLower()}";

        public StoreBossbarExecuteSubCommand(YieldType yield, string bossbarName, Property storeProperty) : base(yield)
        {
            BossbarName = bossbarName;
            StoreProperty = storeProperty;
        }
    }
}
