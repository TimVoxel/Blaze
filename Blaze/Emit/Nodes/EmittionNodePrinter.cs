using Blaze.IO;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
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

                case EmittionNodeKind.GameruleCommand:
                    WriteGameruleCommand((GameruleCommand)node, writer);
                    break;

                case EmittionNodeKind.FunctionCommand:
                    WriteFunctionCommand((FunctionCommand)node, writer);
                    break;

                case EmittionNodeKind.ScoreboardCommand:
                    WriteScoreboardCommand((ScoreboardCommand)node, writer);
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


                // Temporary
                case EmittionNodeKind.TextCommand:
                    WriteTextCommand((TextCommand)node, writer);
                    break;
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
                            var clause = (ScoreboardPlayersCommand.ScoreIdentifierClause)scoreboardPlayers.SubClause;
                            WriteScoreIdentifier(clause.Score, writer);

                            if (scoreboardPlayers.Value != null)
                                writer.WriteNumber($" {scoreboardPlayers.Value}");

                            break;
                        }

                    case ScoreboardPlayersCommand.SubAction.Operations:
                        {
                            var clause = (ScoreboardPlayersCommand.ScoreboardPlayersOperationsClause)scoreboardPlayers.SubClause;
                            WriteScoreIdentifier(clause.Left, writer);
                            writer.WritePunctuation($" {EmittionFacts.GetSignText(clause.Operation)} ");
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
                writer.WriteKeyword(EmittionFacts.GetLocationSyntaxName(dataMerge.Location));
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
            writer.WriteKeyword(EmittionFacts.GetLocationSyntaxName(identifier.Location));
            writer.WriteIdentifier($" {identifier.StorageObject}");
            writer.WriteIdentifier($" {identifier.Path}");
        }

        private static void WriteScoreIdentifier(ScoreIdentifier identifier, IndentedTextWriter writer)
        {
            writer.WriteIdentifier($"{identifier.Selector}");
            writer.WriteIdentifier($" {identifier.Objective}");
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

        private static void WriteGameruleCommand(GameruleCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.GameruleName}");

            if (node.Value != null)
                writer.WriteNumber($" {node.Value}");
            writer.WriteLine();
        }

        private static void WriteFunctionCommand(FunctionCommand node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Keyword);
            writer.WriteIdentifier($" {node.FunctionCallName}");

            if (node.WithClause != null)
            {
                if (node.WithClause.JsonLiteral != null)
                {
                    writer.WriteString(node.WithClause.JsonLiteral);
                }
                else if (node.WithClause.Storage != null)
                {
                    Debug.Assert(node.WithClause.Symbol != null);
                    writer.WriteKeyword(" with storage ");
                    writer.WriteIdentifier(node.WithClause.Storage);
                    writer.WriteIdentifier($" {node.WithClause.Symbol }");
                }
            }
            writer.WriteLine();
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
                writer.WriteNumber(node.Duration);

            if (node.TimeUnits != null)
                writer.WriteString(node.TimeUnits);
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
    }
}
