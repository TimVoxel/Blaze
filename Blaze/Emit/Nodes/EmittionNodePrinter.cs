﻿using Blaze.IO;
using System.CodeDom.Compiler;
using System.Diagnostics;
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

                case EmittionNodeKind.ScoreboardCommand:
                    WriteScoreboardCommand((ScoreboardCommand) node, writer);
                    break;

                case EmittionNodeKind.TextBlock:
                    WriteTextBlock((TextBlockEmittionNode)node, writer);
                    break;

                case EmittionNodeKind.CleanUpMarker:
                    WriteCleanUpMarker((CleanUpMarker) node, writer);
                    break;

                case EmittionNodeKind.FunctionCommand:
                    WriteFunctionCommand((FunctionCommand)node, writer);
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

        private static void WriteCleanUpMarker(CleanUpMarker node, IndentedTextWriter writer)
        {
            writer.WriteLabel("clean up marker");
            writer.WriteLine();
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
