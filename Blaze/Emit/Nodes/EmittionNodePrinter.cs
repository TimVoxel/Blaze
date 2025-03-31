using Blaze.Emit.Data;
using Blaze.IO;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Blaze.Emit.Nodes.Execute;

using static Blaze.Emit.Nodes.ScoreboardObjectivesCommand;

namespace Blaze.Emit.Nodes
{
    internal static class EmittionNodePrinter
    {
        public static void WriteTo(this EmittionNode node, TextWriter writer)
        {
            if (writer is IndentedTextWriter indentedWriter)
                WriteTo(node, indentedWriter);
            else
                WriteTo(node, new IndentedTextWriter(writer));
        }

        public static void WriteTo(this EmittionNode node, IndentedTextWriter writer)
        {
            switch (node.Kind)
            {
                case EmittionNodeKind.Datapack:
                    WriteDatapack((Datapack)node, writer);
                    break;

                case EmittionNodeKind.Namespace:
                    WriteNamespace((NamespaceEmittionNode)node, writer);
                    break;

                case EmittionNodeKind.MinecraftFunction:
                    WriteMinecraftFunction((MinecraftFunction)node, writer);
                    break;

                case EmittionNodeKind.EmptyTrivia:
                    WriteEmptyTrivia((TextTriviaNode) node, writer);
                    break;

                case EmittionNodeKind.LineBreakTrivia:
                    WriteLineBreakTrivia((TextTriviaNode) node, writer);
                    break;

                case EmittionNodeKind.CommentTrivia:
                    WriteCommentTrivia((TextTriviaNode) node, writer);
                    break;

                case EmittionNodeKind.DataCommand:
                    WriteDataCommand((DataCommand)node, writer);
                    break;

                case EmittionNodeKind.DatapackCommand:
                    WriteDatapackCommand((DatapackCommand)node, writer);
                    break;

                case EmittionNodeKind.DifficultyCommand:
                    WriteDifficultyCommand((DifficultyCommand)node, writer);
                    break;

                case EmittionNodeKind.ExecuteCommand:
                    WriteExecuteCommand((ExecuteCommand)node, writer);
                    break;

                case EmittionNodeKind.FunctionCommand:
                    WriteFunctionCommand((FunctionCommand)node, writer);
                    break;

                case EmittionNodeKind.ForceloadCommand:
                    WriteForceloadCommand((ForceloadCommand)node, writer);
                    break;

                case EmittionNodeKind.GameruleCommand:
                    WriteGameruleCommand((GameruleCommand)node, writer);
                    break;

                case EmittionNodeKind.KillCommand:
                    WriteKillCommand((KillCommnad)node, writer);
                    break;

                case EmittionNodeKind.ReturnCommand:
                    WriteReturnCommand((ReturnCommand)node, writer);
                    break;

                case EmittionNodeKind.ScoreboardCommand:
                    WriteScoreboardCommand((ScoreboardCommand)node, writer);
                    break;

                case EmittionNodeKind.SummonCommand:
                    WriteSummonCommand((SummonCommand)node, writer);
                    break;

                case EmittionNodeKind.TagCommand:
                    WriteTagCommand((TagCommand)node, writer);
                    break;

                case EmittionNodeKind.TeleportCommand:
                    WriteTeleportCommand((TeleportCommand)node, writer);
                    break;

                case EmittionNodeKind.TellrawCommand:
                    WriteTellrawCommand((TellrawCommand)node, writer);
                    break;

                case EmittionNodeKind.WeatherCommand:
                    WriteWeatherCommand((WeatherCommand)node, writer);
                    break;

                case EmittionNodeKind.TextBlock:
                    WriteTextBlock((TextBlockEmittionNode)node, writer);
                    break;

                case EmittionNodeKind.MacroCommand:
                    WriteMacroCommand((MacroCommand)node, writer);
                    break;

                case EmittionNodeKind.TextCommand:
                    WriteTextCommand((TextCommand)node, writer);
                    break;

                default:
                    throw new Exception($"Unexpected node kind {node.Kind}");
            }
        }

        private static void WriteDatapack(Datapack node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("datapack ");
            writer.WriteIdentifier(node.Name);
            writer.WriteLine();
            writer.Indent++;

            node.InitFunction.WriteTo(writer);
            node.TickFunction.WriteTo(writer);

            foreach (var ns in node.Namespaces)
                ns.WriteTo(writer);

            writer.Indent--;
        }

        private static void WriteNamespace(NamespaceEmittionNode node, IndentedTextWriter writer)
        {
            Debug.Assert(node.Symbol != null);

            writer.WriteKeyword("namespace ");
            writer.WriteIdentifier(node.Name);
            writer.WriteLine();

            writer.Indent++;

            foreach (var child in node.Children)
                child.WriteTo(writer);

            writer.Indent--;
            writer.WriteLine();
        }

        private static void WriteMinecraftFunction(MinecraftFunction node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("mcfunction ");
            writer.WriteIdentifier(node.Name);
            writer.WriteLine();

            if (node.SubFunctions.Any())
            {
                writer.WriteLabel("sub mcfunctions: ");
                writer.WriteLine();
                writer.Indent++;

                foreach (var child in node.SubFunctions)
                    child.WriteTo(writer);

                writer.Indent--;
            }
            
            if (node.Content.Any())
            {
                writer.WriteLabel("content: ");
                writer.WriteLine();
                writer.Indent++;

                foreach (var textNode in node.Content)
                    textNode.WriteTo(writer);

                writer.Indent--;
            }
            
            writer.WriteLine();
        }

        private static void WriteEmptyTrivia(TextTriviaNode node, IndentedTextWriter writer)
        {
            writer.WriteTrivia("empty trivia");
            writer.WriteLine();
        }

        private static void WriteLineBreakTrivia(TextTriviaNode node, IndentedTextWriter writer)
        {
            writer.WriteTrivia("\\n");
            writer.WriteLine();
        }

        private static void WriteCommentTrivia(TextTriviaNode node, IndentedTextWriter writer)
        {
            writer.WriteTrivia(node.Text);
            writer.WriteLine();
        }
        private static void WriteDataCommand(DataCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);

            if (node is DataGetCommand dataGet)
            {
                writer.WriteKeyword(" get ");
                WriteObjectPathIdentifier(dataGet.Identifier, writer);

                if (dataGet.Multiplier != null)
                    writer.WriteNumber($" {dataGet.Multiplier}");
            }
            else if (node is DataRemoveCommand dataRemove)
            {
                writer.WriteKeyword(" remove ");
                WriteObjectPathIdentifier(dataRemove.Identifier, writer);
            }
            else if (node is DataMergeCommand dataMerge)
            {
                writer.WriteKeyword(" merge ");
                writer.WriteKeyword(EmittionFacts.GetSyntaxName(dataMerge.Location));
                writer.WriteIdentifier($" {dataMerge.StorageObject}");
                writer.WriteString($" {dataMerge.Value}");
            }
            else if (node is DataModifyCommand dataModify)
            {
                writer.WriteKeyword(" modify ");
                WriteObjectPathIdentifier(dataModify.TargetIdentifier, writer);

                writer.WriteKeyword($" {dataModify.Modification.ToString().ToLower()}");

                if (dataModify.Modification == DataModifyCommand.ModificationType.Insert)
                {
                    Debug.Assert(dataModify.InsertIndex != null);
                    writer.WriteNumber($" {dataModify.InsertIndex}");
                }

                if (dataModify.Source is DataModifyCommand.FromSource fromSource)
                {
                    writer.WriteKeyword(" from ");
                    WriteObjectPathIdentifier(fromSource.Identifier, writer);
                }
                else if (dataModify.Source is DataModifyCommand.ValueSource valueSource)
                {
                    writer.WriteKeyword(" value ");
                    writer.WriteString(valueSource.Value);
                }
                else if (dataModify.Source is DataModifyCommand.StringSource stringSource)
                {
                    writer.WriteKeyword(" string ");
                    WriteObjectPathIdentifier(stringSource.Identifier, writer);

                    if (stringSource.StartIndex != null)
                        writer.WriteNumber($" {stringSource.StartIndex}");
                    if (stringSource.EndIndex != null)
                        writer.WriteNumber($" {stringSource.EndIndex}");
                }
                else
                    throw new Exception($"Unexpected data modify source {node.GetType()}");
            }
            else
                throw new Exception($"Unexpected data sub command {node.GetType()}");

            writer.WriteLine();
        }

        private static void WriteObjectPathIdentifier(ObjectPathIdentifier identifier, IndentedTextWriter writer)
        {
            writer.WriteKeyword(EmittionFacts.GetSyntaxName(identifier.Location));
            writer.WriteIdentifier($" {identifier.StorageObject}");
            writer.WriteIdentifier($" {identifier.Path}");
        }

        private static void WriteScoreIdentifier(ScoreIdentifier identifier, IndentedTextWriter writer)
        {
            writer.WriteIdentifier($"{identifier.Selector}");
            writer.WriteIdentifier($" {identifier.Objective}");
        }

        private static void WriteCoords2(Coordinates2 coords, IndentedTextWriter writer)
        {
            writer.WriteNumber($"{coords.X} ");
            writer.WriteNumber(coords.Z);
        }

        private static void WriteCoords3(Coordinates3 coords, IndentedTextWriter writer)
        {
            writer.WriteNumber(coords.X);
            writer.WriteNumber($" {coords.Y} ");
            writer.WriteNumber(coords.Z);
        }

        private static void WriteDatapackCommand(DatapackCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);

            if (node is DatapackEnableCommand enableCommand)
            {
                writer.WriteKeyword(" enable ");
                writer.WriteIdentifier(enableCommand.PackFullName);

                if (enableCommand.Mode != null)
                {
                    writer.WriteKeyword($" {enableCommand.Mode?.ToString().ToLower()}");
                    
                    if (enableCommand.Mode != DatapackEnableCommand.EnableMode.First &&
                        enableCommand.Mode != DatapackEnableCommand.EnableMode.Last)
                    {
                        Debug.Assert(enableCommand.OtherPackFullName != null);
                        writer.WriteIdentifier($" {enableCommand.OtherPackFullName}");
                    }
                }
               
            }
            else if (node is DatapackDisableCommand disableCommand)
            {
                writer.WriteKeyword($" disable ");
                writer.WriteIdentifier(disableCommand.PackFullName);
            }
            else if (node is DatapackListCommand listCommand)
            {
                writer.WriteKeyword($" list");
                if (listCommand.Filter != null)
                    writer.WriteKeyword($" {listCommand.Filter?.ToString().ToLower()}");
            }
            writer.WriteLine();
        }

        private static void WriteDifficultyCommand(DifficultyCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            if (node.Value != null)
                writer.WriteKeyword($" {node.Value}");
            writer.WriteLine();
        }

        private static void WriteExecuteCommand(ExecuteCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);

            foreach (var subCommand in node.SubCommands)
            {
                writer.Write(" ");
                subCommand.WriteTo(writer);
            }

            writer.WriteKeyword($" run ");
            node.RunCommand.WriteTo(writer);
        }

        private static void WriteTo(this ExecuteSubCommand subCommand, IndentedTextWriter writer)
        {
            switch (subCommand.Kind)
            {
                case ExecuteSubCommandKind.Align:
                    WriteAlign((AlignExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.Anchored:
                    WriteAnchored((AnchoredExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.As:
                    WriteAs((AsExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.At:
                    WriteAt((AtExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.Facing:
                    WriteFacing((FacingExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.If:
                    WriteIf((IfExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.In:
                    WriteIn((InExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.On:
                    WriteOn((OnExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.Positioned:
                    WritePositioned((PositionedExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.PositionedAs:
                    WritePositionedAs((PositionedAsExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.PositionedOver:
                    WritePositionedOver((PositionedOverExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.Rotated:
                    WriteRotated((RotatedExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.RotatedAs:
                    WriteRotatedAs((RotatedAsExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.StorePath:
                    WriteStorePath((StorePathExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.StoreScore:
                    WriteStoreScore((StoreScoreExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.StoreBossbar:
                    WriteStoreBossbar((StoreBossbarExecuteSubCommand)subCommand, writer);
                    break;

                case ExecuteSubCommandKind.Unless:
                    WriteUnless((UnlessExecuteSubCommand)subCommand, writer);
                    break;

                default:
                    throw new Exception($"Unexpected sub command type {subCommand.GetType()}");

            }
        }

        private static void WriteAlign(AlignExecuteSubCommand alignCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("align");
            writer.WriteIdentifier($" {alignCommand.Axis}");
        }

        private static void WriteAnchored(AnchoredExecuteSubCommand anchoredCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("anchored");
            writer.WriteKeyword($" {anchoredCommand.Anchor.ToString().ToLower()}");
        }

        private static void WriteAs(AsExecuteSubCommand asCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("as");
            writer.WriteIdentifier($" {asCommand.Selector}");
        }

        private static void WriteAt(AtExecuteSubCommand atCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("at");
            writer.WriteIdentifier($" {atCommand.Selector}");
        }

        private static void WriteFacing(FacingExecuteSubCommand facingCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("facing ");
            WriteRotationClause(facingCommand.RotationClause, writer);
        }

        private static void WriteIf(IfExecuteSubCommand ifCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("if ");
            ifCommand.ConditionClause.WriteTo(writer);
        }

        private static void WriteIn(InExecuteSubCommand inCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("in");
            writer.WriteString($" {inCommand.World}");
        }

        private static void WriteOn(OnExecuteSubCommand onCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("on");
            writer.WriteKeyword($" {onCommand.Relation.ToString().ToLower()}");
        }

        private static void WritePositioned(PositionedExecuteSubCommand positionedCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("positioned ");
            WriteCoords3(positionedCommand.Coords, writer);
        }

        private static void WritePositionedAs(PositionedAsExecuteSubCommand positionedAsCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("positioned as");
            writer.WriteIdentifier($" {positionedAsCommand.Selector}");
        }

        private static void WritePositionedOver(PositionedOverExecuteSubCommand positionedOverCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("positioned over");
            writer.WriteKeyword($" {positionedOverCommand.HeightMap.ToString().ToLower()}");
        }

        private static void WriteRotated(RotatedExecuteSubCommand rotatedCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("rotated ");
            WriteCoords2(rotatedCommand.Rotation, writer);
        }

        private static void WriteRotatedAs(RotatedAsExecuteSubCommand rotatedAsCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("rotated as ");
            writer.WriteIdentifier(rotatedAsCommand.Selector);
        }

        private static void WriteStorePath(StorePathExecuteSubCommand storePathCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("store");
            writer.WriteKeyword($" {storePathCommand.Yield.ToString().ToLower()} ");
            WriteObjectPathIdentifier(storePathCommand.Identifier, writer);
            writer.WriteKeyword($" {storePathCommand.ConvertType.ToString().ToLower()}");
            writer.WriteNumber($" {storePathCommand.Scale}");
        }
        
        private static void WriteStoreScore(StoreScoreExecuteSubCommand storeScoreCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("store ");
            writer.WriteKeyword($" {storeScoreCommand.Yield.ToString().ToLower()}");
            writer.Write(" score ");
            WriteScoreIdentifier(storeScoreCommand.Identifier, writer);
        }

        private static void WriteStoreBossbar(StoreBossbarExecuteSubCommand storeBossbarCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("store");
            writer.WriteKeyword($" {storeBossbarCommand.Yield.ToString().ToLower()}");
            writer.WriteKeyword(" bossbar");
            writer.WriteString($" {storeBossbarCommand.BossbarName}");
            writer.WriteKeyword($" {storeBossbarCommand.StoreProperty.ToString().ToLower()}");
        }

        private static void WriteUnless(UnlessExecuteSubCommand unlessCommand, IndentedTextWriter writer)
        {
            writer.WriteKeyword("unless ");
            unlessCommand.ConditionClause.WriteTo(writer);
        }

        private static void WriteTo(this ExecuteConditionalClause conditionalClause, IndentedTextWriter writer)
        {
            switch (conditionalClause)
            {
                case IfBiomeClause biomeClause:
                    WriteIfBiomeClause(biomeClause, writer);
                    break;
                case IfBlockClause blockClause:
                    WriteIfBlockClause(blockClause, writer);
                    break;
                case IfBlocksClause blocksClause:
                    WriteIfBlocksClause(blocksClause, writer);
                    break;
                case IfDataClause dataClause:
                    WriteIfDataClause(dataClause, writer);
                    break;
                case IfDimensionClause dimensionClause:
                    WriteIfDimensionClause(dimensionClause, writer);
                    break;
                case IfEntityClause entityClause:
                    WriteIfEntityClause(entityClause, writer);
                    break;
                case IfFunctionClause functionClause:
                    WriteIfFunctionClause(functionClause, writer);
                    break;
                case IfItemsBlockClause itemsBlockClause:
                    WriteIfItemsBlockClause(itemsBlockClause, writer);
                    break;
                case IfItemsEntityClause itemsEntityClause:
                    WriteIfItemsEntityClause(itemsEntityClause, writer);
                    break;
                case IfLoadedClause loadedClause:
                    WriteIfLoadedClause(loadedClause, writer);
                    break;
                case IfPredicateClause predicateClause:
                    WriteIfPredicateClause(predicateClause, writer);
                    break;
                case IfScoreClause scoreClause:
                    WriteIfScoreClause(scoreClause, writer);
                    break;
                case IfScoreMatchesClause scoreMatchesClause:
                    WriteIfScoreMatchesClause(scoreMatchesClause, writer);
                    break;
                default:
                    throw new Exception($"Unexpected conditional clause type {conditionalClause.GetType()}");
            }
        }

        private static void WriteIfBiomeClause(IfBiomeClause biomeClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("biome");
            WriteCoords3(biomeClause.Position, writer);
            writer.WriteString($" {biomeClause.Biome}");
        }

        private static void WriteIfBlockClause(IfBlockClause blockClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("block ");
            WriteCoords3(blockClause.Position, writer);
            writer.WriteString($" {blockClause.Block}");
        }

        private static void WriteIfBlocksClause(IfBlocksClause blocksClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("blocks ");
            WriteCoords3(blocksClause.FirstStart, writer);
            writer.Write(" ");
            WriteCoords3(blocksClause.FirstEnd, writer);
            writer.Write(" ");
            WriteCoords3(blocksClause.SecondStart, writer);
            writer.WriteKeyword($" {blocksClause.Mode.ToString().ToLower()}");
        }

        private static void WriteIfDataClause(IfDataClause dataClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("data ");
            WriteObjectPathIdentifier(dataClause.PathIdentifier, writer);
        }

        private static void WriteIfDimensionClause(IfDimensionClause dimensionClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("dimension");
            writer.WriteString($" {dimensionClause.Dimension}");
        }

        private static void WriteIfEntityClause(IfEntityClause entityClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("entity");
            writer.WriteIdentifier($" {entityClause.Selector}");
        }

        private static void WriteIfFunctionClause(IfFunctionClause functionClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("function");
            writer.WriteIdentifier($" {functionClause.FunctionCallName}");
        }

        private static void WriteIfItemsBlockClause(IfItemsBlockClause itemsBlockClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("items block ");
            WriteCoords3(itemsBlockClause.Position, writer);
            writer.WriteIdentifier($" {itemsBlockClause.Slots}");
            writer.WriteString($" {itemsBlockClause.ItemPredicate}");
        }

        private static void WriteIfItemsEntityClause(IfItemsEntityClause itemsEntityClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("items entity");
            writer.WriteIdentifier($" {itemsEntityClause.Selector}");
            writer.WriteIdentifier($" {itemsEntityClause.Slots}");
            writer.WriteString($" {itemsEntityClause.ItemPredicate}");
        }

        private static void WriteIfLoadedClause(IfLoadedClause loadedClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("loaded ");
            WriteCoords3(loadedClause.Position, writer);
        }

        private static void WriteIfPredicateClause(IfPredicateClause predicateClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("predicate");
            writer.WriteString($" {predicateClause.PredicateName}");
        }

        private static void WriteIfScoreClause(IfScoreClause scoreClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("score ");
            WriteScoreIdentifier(scoreClause.Left, writer);
            writer.WritePunctuation($" {scoreClause.Comparison.GetSign()} ");
            WriteScoreIdentifier(scoreClause.Right, writer);
        }

        private static void WriteIfScoreMatchesClause(IfScoreMatchesClause scoreMatchesClause, IndentedTextWriter writer)
        {
            writer.WriteKeyword("score ");
            WriteScoreIdentifier(scoreMatchesClause.Identifier, writer);
            writer.WriteKeyword($" matches ");
            writer.WriteNumber(EmittionFacts.GetRangeString(scoreMatchesClause.LowerBound, scoreMatchesClause.UpperBound));
        }

        private static void WriteFunctionCommand(FunctionCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.FunctionCallName}");

            if (node.WithClause != null)
            {
                switch (node.WithClause)
                {
                    case FunctionCommand.FunctionWithPathIdentifierClause pathClause:
                        writer.WriteKeyword(" with ");
                        WriteObjectPathIdentifier(pathClause.Identifier, writer);
                        break;

                    case FunctionCommand.FunctionWithArgumentsClause argumentsClause:
                        writer.WriteString($" {argumentsClause.Arguments}");
                        break;

                    default:
                        throw new Exception($"Unexpected with clause type {node.GetType()}");
                }
            }
            writer.WriteLine();
        }

        private static void WriteForceloadCommand(ForceloadCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteKeyword($" {node.Action.ToString().ToLower()}");

            switch (node.Action)
            {
                case ForceloadCommand.SubAction.Add:
                    Debug.Assert(node.Start != null);
                    writer.Write(" "); 
                    WriteCoords2(node.Start, writer);
                    if (node.End != null)
                        WriteCoords2(node.End, writer);
                    break;

                case ForceloadCommand.SubAction.Remove:
                    Debug.Assert(node.Start != null);
                    writer.Write(" ");
                    WriteCoords2(node.Start, writer);
                    if (node.RemoveAll != null && (bool)node.RemoveAll)
                        writer.WriteKeyword(" all");
                    else if (node.End != null)
                    {
                        writer.Write(" ");
                        WriteCoords2(node.End, writer);

                    }
                    break;

                case ForceloadCommand.SubAction.Query:
                    if (node.Start != null)
                    {
                        writer.Write(" ");
                        WriteCoords2(node.Start, writer);
                    }
                    break;
            }

            writer.WriteLine();
        }

        private static void WriteGameruleCommand(GameruleCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.GameruleName}");

            if (node.Value != null)
                writer.WriteNumber($" {node.Value}");
            writer.WriteLine();
        }

        private static void WriteKillCommand(KillCommnad node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.Selector}");
            writer.WriteLine();
        }

        private static void WriteReturnCommand(ReturnCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);

            if (node is ReturnRunCommand run)
            {
                writer.WriteKeyword($" run ");
                run.Command.WriteTo(writer);
            }
            else if (node is ReturnValueCommand value)
            {
                writer.WriteString($" {value.Value}");
                writer.WriteLine();
            }
            else
                throw new Exception($"Unexpected return command type {node.GetType()}");
        }

        private static void WriteScoreboardCommand(ScoreboardCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword($"{node.Keyword} ");

            if (node is ScoreboardObjectivesCommand scoreboardObjetives)
            {
                writer.WriteKeyword("objectives ");
                writer.WriteKeyword($"{scoreboardObjetives.Action.ToString().ToLower()} ");

                switch (scoreboardObjetives.Action)
                {
                    case SubAction.Add:
                        writer.WriteIdentifier($"{scoreboardObjetives.Objective} ");
                        writer.WriteIdentifier($"{scoreboardObjetives.Criteria} ");
                        writer.WriteString($"{scoreboardObjetives.DisplayName}");
                        break;
                    case SubAction.Remove:
                        writer.WriteIdentifier($"{scoreboardObjetives.Objective}");
                        break;
                    case SubAction.Modify:
                        writer.WriteIdentifier($"{scoreboardObjetives.Objective} ");
                        writer.WriteIdentifier($"{scoreboardObjetives.ModifiedProperty} ");
                        writer.WriteIdentifier($"{scoreboardObjetives.ModifyValue}");
                        break;
                    case SubAction.SetDisplay:
                        writer.WriteKeyword($"{scoreboardObjetives.DisplaySlot} ");
                        writer.WriteIdentifier($"{scoreboardObjetives.Objective}");
                        break;
                }
            }
            else if (node is ScoreboardPlayersCommand scoreboardPlayers)
            {
                writer.WriteKeyword("players ");
                writer.WriteKeyword($"{scoreboardPlayers.Action.ToString().ToLower()} ");

                switch (scoreboardPlayers.Action)
                {
                    case ScoreboardPlayersCommand.SubAction.Add:
                    case ScoreboardPlayersCommand.SubAction.Remove:
                    case ScoreboardPlayersCommand.SubAction.Set:
                    case ScoreboardPlayersCommand.SubAction.Get:
                    case ScoreboardPlayersCommand.SubAction.Enable:
                    case ScoreboardPlayersCommand.SubAction.Reset:
                        {
                            WriteScoreIdentifier((ScoreIdentifier)scoreboardPlayers.SubClause, writer);

                            if (scoreboardPlayers.Value != null)
                                writer.WriteNumber($" {scoreboardPlayers.Value}");

                            break;
                        }

                    case ScoreboardPlayersCommand.SubAction.Operation:
                        {
                            var clause = (ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause)scoreboardPlayers.SubClause;
                            WriteScoreIdentifier(clause.Left, writer);
                            writer.WritePunctuation($" {EmittionFacts.GetSign(clause.Operation)} ");
                            WriteScoreIdentifier(clause.Right, writer);
                            break;
                        }

                    case ScoreboardPlayersCommand.SubAction.List:
                        {
                            var clause = (ScoreboardPlayersCommand.ListTarget)scoreboardPlayers.SubClause;
                            if (!string.IsNullOrEmpty(clause.Text))
                                writer.WriteIdentifier(clause.Text);
                            break;
                        }

                    case ScoreboardPlayersCommand.SubAction.Display:
                        {
                            if (scoreboardPlayers.SubClause is ScoreboardPlayersCommand.DisplayNameClause nameClause)
                            {
                                WriteScoreIdentifier(nameClause.Score, writer);
                                writer.WriteString($" {nameClause.Name}");
                            }
                            else if (scoreboardPlayers.SubClause is ScoreboardPlayersCommand.DisplayNumberFormatClause numberFormatClause)
                            {
                                WriteScoreIdentifier(numberFormatClause.Score, writer);
                                writer.WriteKeyword($" {numberFormatClause.Format.ToString().ToLower()}");

                                if (numberFormatClause.Format == ScoreboardPlayersCommand.DisplayNumberFormatClause.NumberFormat.Styled)
                                {
                                    Debug.Assert(numberFormatClause.Style != null);
                                    writer.WriteString($" {numberFormatClause.Style}");
                                }
                            }
                            break;
                        }
                }
            }
            else
                throw new Exception($"Unexpected scoreboard sub command {node.GetType()}");

            writer.WriteLine();
        }

        private static void WriteSummonCommand(SummonCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.EntityType} ");
            WriteCoords3(node.Location, writer);

            if (node.Nbt != null)
                writer.WriteString($" {node.Nbt}");

            writer.WriteLine();
        }

        private static void WriteTagCommand(TagCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.Selector}");
            writer.WriteKeyword($" {node.Action.ToString().ToLower()}");
            
            if (node.TagName != null)
                writer.WriteString($" {node.TagName}");
            writer.WriteLine();
        }

        private static void WriteTeleportCommand(TeleportCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.TargetSelector}");

            if (node is TeleportToEntityCommand tpEntity)
                writer.WriteIdentifier($" {tpEntity.DestinationEntitySelector}");
            else if (node is TeleportToLocationCommand tpLocation)
            {
                writer.Write(" ");
                WriteCoords3(tpLocation.Location, writer);
                
                if (tpLocation.RotationClause != null)
                {
                    writer.Write(" ");
                    WriteRotationClause(tpLocation.RotationClause, writer);
                }
            }
            writer.WriteLine();
        }

        private static void WriteRotationClause(IRotationClause rotationClause, IndentedTextWriter writer)
        {
            if (rotationClause is Coordinates2 pitchYaw)
                WriteCoords2(pitchYaw, writer);
            else if (rotationClause is FacingLocationClause facingLocation)
            {
                writer.WriteKeyword("facing ");
                WriteCoords3(facingLocation.Location, writer);
            }
            else if (rotationClause is FacingEntityClause facingEntity)
            {
                writer.WriteKeyword("facing");
                writer.WriteKeyword(" entity ");
                writer.WriteIdentifier(facingEntity.Selector);

                if (facingEntity.Anchor != null)
                    writer.WriteKeyword($" {facingEntity.Anchor?.ToString().ToLower()}");
            }
        } 

        private static void WriteTellrawCommand(TellrawCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.Selector} ");
            writer.WriteString(node.Component);
            writer.WriteLine();
        }
        
        private static void WriteWeatherCommand(WeatherCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteKeyword($" {node.WeatherType}");

            if (node.Duration != null)
                writer.WriteNumber($" {node.Duration}");

            if (node.TimeUnits != null)
                writer.WriteString(node.TimeUnits);

            writer.WriteLine();
        }

        private static void WriteTextBlock(TextBlockEmittionNode node, IndentedTextWriter writer)
        {
            writer.WriteLabel("block: ");
            writer.WriteLine();
            writer.Indent++;

            foreach (var n in node.Lines)
                n.WriteTo(writer);

            writer.Indent--;
        }

        private static void WriteTextCommand(TextCommand node, IndentedTextWriter writer)
        {
            writer.WriteLine(node.Text);
        }

        private static void WriteMacroCommand(MacroCommand node, IndentedTextWriter writer)
        {
            writer.WritePunctuation("$");
            node.Command.WriteTo(writer);
        }
    }
}
