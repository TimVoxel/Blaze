using Blaze.Binding;
using Blaze.Emit.Data;
using Blaze.Emit.Nodes;
using Blaze.Emit.Nodes.Execute;
using Blaze.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using static Blaze.Emit.Nodes.Execute.StoreExecuteSubCommand;

namespace Blaze.Emit
{
    internal sealed class CommandNodeFactory
    {
        internal class ExecuteCommandBuilder
        {
            public CommandNodeFactory Factory { get; }
            public ImmutableArray<ExecuteSubCommand>.Builder SubCommands { get; }

            public ExecuteCommandBuilder(CommandNodeFactory factory)
            {
                SubCommands = ImmutableArray.CreateBuilder<ExecuteSubCommand>();
                Factory = factory;
            }

            internal ExecuteCommandBuilder Align(string axis)
            {
                SubCommands.Add(new AlignExecuteSubCommand(axis));
                return this;
            }

            internal ExecuteCommandBuilder Anchored(FacingAnchor anchor)
            {
                SubCommands.Add(new AnchoredExecuteSubCommand(anchor));
                return this;
            }

            internal ExecuteCommandBuilder As(string selector)
            {
                SubCommands.Add(new AsExecuteSubCommand(selector));
                return this;
            }

            internal ExecuteCommandBuilder As(UUID uuid)
            {
                SubCommands.Add(new AsExecuteSubCommand(uuid.ToString()));
                return this;
            }

            internal ExecuteCommandBuilder At(string selector)
            {
                SubCommands.Add(new AtExecuteSubCommand(selector));
                return this;
            }

            internal ExecuteCommandBuilder Facing(string selector, FacingAnchor anchor = FacingAnchor.Eyes)
            {
                SubCommands.Add(new FacingExecuteSubCommand(new FacingEntityClause(selector, anchor)));
                return this;
            }

            internal ExecuteCommandBuilder Facing(string x, string y, string z)
            {
                SubCommands.Add(new FacingExecuteSubCommand(new FacingLocationClause(new Coordinates3(x, y, z))));
                return this;
            }

            internal ExecuteCommandBuilder In(string world)
            {
                SubCommands.Add(new InExecuteSubCommand(world));
                return this;
            }

            internal ExecuteCommandBuilder On(OnExecuteSubCommand.RelationType relation)
            {
                SubCommands.Add(new OnExecuteSubCommand(relation));
                return this;
            }

            internal ExecuteCommandBuilder IfBiome(Coordinates3 position, string biome)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfBiomeClause(position, biome)));
                return this;
            }

            internal ExecuteCommandBuilder IfBlock(Coordinates3 position, string block)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfBlockClause(position, block)));
                return this;
            }

            internal ExecuteCommandBuilder IfBlocks(Coordinates3 firstStart, Coordinates3 firstEnd, Coordinates3 secondStart, IfBlocksClause.MatchMode mode)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfBlocksClause(firstStart, firstEnd, secondStart, mode)));
                return this;
            }

            internal ExecuteCommandBuilder IfData(DataLocation location, string obj, string path)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfDataClause(new ObjectPathIdentifier(location, obj, path))));
                return this;
            }

            internal ExecuteCommandBuilder IfDimension(string dimension)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfDimensionClause(dimension)));
                return this;
            }

            internal ExecuteCommandBuilder IfEntity(string selector)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfEntityClause(selector)));
                return this;
            }

            internal ExecuteCommandBuilder IfFunction(string functionCallName)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfFunctionClause(functionCallName)));
                return this;
            }

            internal ExecuteCommandBuilder IfItemsBlock(Coordinates3 position, string slots, string itemPredicate)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfItemsBlockClause(position, slots, itemPredicate)));
                return this;
            }

            internal ExecuteCommandBuilder IfItemsEntity(string selector, string slots, string itemPredicate)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfItemsEntityClause(selector, slots, itemPredicate)));
                return this;
            }

            internal ExecuteCommandBuilder IfLoaded(Coordinates3 position)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfLoadedClause(position)));
                return this;
            }

            internal ExecuteCommandBuilder IfPredicate(string predicateName)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfPredicateClause(predicateName)));
                return this;
            }

            internal ExecuteCommandBuilder IfScore(ScoreIdentifier left, IfScoreClause.ComparisonType comparison, ScoreIdentifier right)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfScoreClause(left, comparison, right)));
                return this;
            }

            internal ExecuteCommandBuilder IfScore(EmittionVariableSymbol left, BoundBinaryOperatorKind operatorKind, EmittionVariableSymbol right)
            {
                var comparison = EmittionFacts.ToComparisonType(operatorKind);
                SubCommands.Add(new IfExecuteSubCommand(new IfScoreClause(Factory.ToScoreIdentifier(left), comparison, Factory.ToScoreIdentifier(right))));
                return this;
            }

            internal ExecuteCommandBuilder IfScoreMatches(ScoreIdentifier identifier, string? lowerBound, string? upperBound)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfScoreMatchesClause(identifier, lowerBound, upperBound)));
                return this;
            }

            internal ExecuteCommandBuilder IfScoreMatches(EmittionVariableSymbol variable, string? lowerBound, string? upperBound)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfScoreMatchesClause(Factory.ToScoreIdentifier(variable), lowerBound, upperBound)));
                return this;
            }

            internal ExecuteCommandBuilder IfScoreMatches(EmittionVariableSymbol variable, string exactValue)
            {
                SubCommands.Add(new IfExecuteSubCommand(new IfScoreMatchesClause(Factory.ToScoreIdentifier(variable), exactValue, exactValue)));
                return this;
            }

            internal ExecuteCommandBuilder Positioned(Coordinates3 coordinates)
            {
                SubCommands.Add(new PositionedExecuteSubCommand(coordinates));
                return this;
            }

            internal ExecuteCommandBuilder Positioned(string x, string y, string z)
            {
                SubCommands.Add(new PositionedExecuteSubCommand(new Coordinates3(x, y, z)));
                return this;
            }

            internal ExecuteCommandBuilder PositionedAs(string selector)
            {
                SubCommands.Add(new PositionedAsExecuteSubCommand(selector));
                return this;
            }

            internal ExecuteCommandBuilder PositionedOver(PositionedOverExecuteSubCommand.HeightMapType heightMap)
            {
                SubCommands.Add(new PositionedOverExecuteSubCommand(heightMap));
                return this;
            }

            internal ExecuteCommandBuilder Rotated(Coordinates2 rotation)
            {
                SubCommands.Add(new RotatedExecuteSubCommand(rotation));
                return this;
            }

            internal ExecuteCommandBuilder RotatedAs(string selector)
            {
                SubCommands.Add(new RotatedAsExecuteSubCommand(selector));
                return this;
            }

            internal ExecuteCommandBuilder StorePath(YieldType yield, ObjectPathIdentifier identifier, StoreType convertType, string scale)
            {
                SubCommands.Add(new StorePathExecuteSubCommand(yield, identifier, convertType, scale));
                return this;
            }

            internal ExecuteCommandBuilder StoreResultStorage(EmittionVariableSymbol variable, StoreType convertType, string scale)
            {
                SubCommands.Add(new StorePathExecuteSubCommand(YieldType.Result, Factory.ToObjectPathIdentifier(variable), convertType, scale));
                return this;
            }

            internal ExecuteCommandBuilder StoreScore(YieldType yield, string name, string score)
            {
                SubCommands.Add(new StoreScoreExecuteSubCommand(yield, new ScoreIdentifier(name, score)));
                return this;
            }

            internal ExecuteCommandBuilder StoreScore(YieldType yield, EmittionVariableSymbol variable)
            {
                SubCommands.Add(new StoreScoreExecuteSubCommand(yield, Factory.ToScoreIdentifier(variable)));
                return this;
            }

            internal ExecuteCommandBuilder StoreResultScore(EmittionVariableSymbol variable)
            {
                SubCommands.Add(new StoreScoreExecuteSubCommand(YieldType.Result, Factory.ToScoreIdentifier(variable)));
                return this;
            }

            internal ExecuteCommandBuilder StoreBossbar(YieldType yield, string bossbarName, StoreBossbarExecuteSubCommand.Property storeProperty)
            {
                SubCommands.Add(new StoreBossbarExecuteSubCommand(yield, bossbarName, storeProperty));
                return this;
            }

            internal ExecuteCommandBuilder UnlessBiome(Coordinates3 position, string biome)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfBiomeClause(position, biome)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessBlock(Coordinates3 position, string block)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfBlockClause(position, block)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessBlocks(Coordinates3 firstStart, Coordinates3 firstEnd, Coordinates3 secondStart, IfBlocksClause.MatchMode mode)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfBlocksClause(firstStart, firstEnd, secondStart, mode)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessData(DataLocation location, string obj, string path)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfDataClause(new ObjectPathIdentifier(location, obj, path))));
                return this;
            }

            internal ExecuteCommandBuilder UnlessDimension(string dimension)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfDimensionClause(dimension)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessEntity(string selector)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfEntityClause(selector)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessFunction(string functionCallName)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfFunctionClause(functionCallName)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessItemsBlock(Coordinates3 position, string slots, string itemPredicate)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfItemsBlockClause(position, slots, itemPredicate)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessItemsEntity(string selector, string slots, string itemPredicate)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfItemsEntityClause(selector, slots, itemPredicate)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessLoaded(Coordinates3 position)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfLoadedClause(position)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessPredicate(string predicateName)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfPredicateClause(predicateName)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessScore(ScoreIdentifier left, IfScoreClause.ComparisonType comparison, ScoreIdentifier right)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfScoreClause(left, comparison, right)));
                return this;
            }

            internal ExecuteCommandBuilder UnlessScoreMatches(ScoreIdentifier identifier, string? lowerBound, string? upperBound)
            {
                SubCommands.Add(new UnlessExecuteSubCommand(new IfScoreMatchesClause(identifier, lowerBound, upperBound)));
                return this;
            }

            public ExecuteCommand Run(CommandNode runCommand) => new ExecuteCommand(SubCommands.ToImmutable(), runCommand);
        }

        private string VarsScoreboard { get; }
        private string MainStorage { get; }
        private string ConstStorage { get; }

        public CommandNodeFactory(string varsScoreboard, string mainStorage, string constStorage)
        {
            VarsScoreboard = varsScoreboard;
            MainStorage = mainStorage;
            ConstStorage = constStorage;
        }

        internal ScoreIdentifier ToScoreIdentifier(EmittionVariableSymbol variable)
        {
            Debug.Assert(variable.Location == DataLocation.Scoreboard, $"{variable.SaveName}: {variable.Location}");
            return new ScoreIdentifier(variable.SaveName, VarsScoreboard);
        }

        internal ObjectPathIdentifier ToObjectPathIdentifier(EmittionVariableSymbol variable)
        {
            Debug.Assert(variable.Location == DataLocation.Storage, $"{variable.SaveName}: {variable.Location}");
            return new ObjectPathIdentifier(DataLocation.Storage, MainStorage, variable.SaveName);
        }

        internal ObjectPathIdentifier ToStoragePathIdentifier(string name) => new ObjectPathIdentifier(DataLocation.Storage, MainStorage, name);

        internal ExecuteCommandBuilder CreateExecuteBuilder() => new ExecuteCommandBuilder(this);    
    }
}
