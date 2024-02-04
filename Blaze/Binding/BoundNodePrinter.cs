using Blaze.IO;
using Blaze.Symbols;
using System.CodeDom.Compiler;

namespace Blaze.Binding
{
    internal static class BoundNodePrinter
    {
        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            if (writer is IndentedTextWriter)
                WriteTo(node, writer);
            else
                WriteTo(node, new IndentedTextWriter(writer));
        }

        public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    WriteError((BoundErrorExpression)node, writer);
                    break;
                case BoundNodeKind.LiteralExpression:
                    WriteLiteralExpression((BoundLiteralExpression)node, writer);
                    break;
                case BoundNodeKind.VariableExpression:
                    WriteVariableExpression((BoundVariableExpression)node, writer);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    WriteAssignmentExpression((BoundAssignmentExpression)node, writer);
                    break;
                case BoundNodeKind.UnaryExpression:
                    WriteUnaryExpression((BoundUnaryExpression)node, writer);
                    break;
                case BoundNodeKind.BinaryExpression:
                    WriteBinaryExpression((BoundBinaryExpression)node, writer);
                    break;
                case BoundNodeKind.CallExpression:
                    WriteCallExpression((BoundCallExpression)node, writer);
                    break;
                case BoundNodeKind.ConversionExpression:
                    WriteConversionExpression((BoundConversionExpression)node, writer);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    WriteExpressionStatement((BoundExpressionStatement)node, writer);
                    break;
                case BoundNodeKind.BlockStatement:
                    WriteBlockStatement((BoundBlockStatement)node, writer);
                    break;
                case BoundNodeKind.GoToStatement:
                    WriteGotoStatement((BoundGotoStatement)node, writer);
                    break;
                case BoundNodeKind.LabelStatement:
                    WriteLabel((BoundLabelStatement)node, writer);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    WriteConditionalGotoStatement((BoundConditionalGotoStatement)node, writer);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    WriteVariableDeclaration((BoundVariableDeclarationStatement)node, writer);
                    break;
                case BoundNodeKind.IfStatement:
                    WriteIfStatement((BoundIfStatement)node, writer);
                    break;
                case BoundNodeKind.WhileStatement:
                    WriteWhileStatement((BoundWhileStatement)node, writer);
                    break;
                case BoundNodeKind.DoWhileStatement:
                    WriteDoWhileStatement((BoundDoWhileStatement)node, writer);
                    break;
                case BoundNodeKind.ForStatement:
                    WriteForStatement((BoundForStatement)node, writer);
                    break;
                case BoundNodeKind.ReturnStatement:
                    WriteReturnStatement((BoundReturnStatement)node, writer);
                    break;
                default:
                    throw new Exception($"Unexpected node kind {node.Kind}");
            }
        }

        private static void WriteReturnStatement(BoundReturnStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("return ");
            if (node.Expression != null)
                node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteNestedStatement(this IndentedTextWriter writer, BoundStatement node)
        {
            bool needsIndentation = !(node is BoundBlockStatement);
            
            if (needsIndentation)
                writer.Indent++;

            node.WriteTo(writer);

            if (needsIndentation)
                writer.Indent--;
        }

        private static void WriteNestedExpression(this IndentedTextWriter writer, int precedence, BoundExpression node)
        {
            if (node is BoundUnaryExpression unary)
                writer.WriteNestedExpression(precedence, SyntaxFacts.GetUnaryOperatorPrecedence(unary.Operator.SyntaxKind), unary);
            else if (node is BoundBinaryExpression binary)
                writer.WriteNestedExpression(precedence, SyntaxFacts.GetBinaryOperatorPrecedence(binary.Operator.SyntaxKind), binary);
            else
                node.WriteTo(writer);
        }

        private static void WriteNestedExpression(this IndentedTextWriter writer, int parentPrecedence, int currentPrecedence, BoundExpression node)
        {
            var needsParenthesis = parentPrecedence >= currentPrecedence;
            if (needsParenthesis)
                writer.WritePunctuation("(");

            node.WriteTo(writer);

            if (needsParenthesis)
                writer.WritePunctuation(")");
        }

        private static void WriteBlockStatement(BoundBlockStatement node, IndentedTextWriter writer)
        {
            writer.WritePunctuation("{");
            writer.WriteLine();
            writer.Indent++;

            foreach (var s in node.Statements)
                s.WriteTo(writer);

            writer.Indent--;
            writer.WritePunctuation("}");
            writer.WriteLine();
        }

        private static void WriteVariableDeclaration(BoundVariableDeclarationStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("let ");
            writer.WriteIdentifier(node.Variable.Name);
            writer.WritePunctuation(" = ");
            node.Initializer.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteIfStatement(BoundIfStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("if ");
            writer.WritePunctuation("(");
            node.Condition.WriteTo(writer);
            writer.WritePunctuation(")");
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
            if (node.ElseBody != null)
            {
                writer.WriteKeyword("else ");
                writer.WriteLine();
                writer.WriteNestedStatement(node.ElseBody);
            }
        }

        private static void WriteWhileStatement(BoundWhileStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("while ");
            writer.WritePunctuation("(");
            node.Condition.WriteTo(writer);
            writer.WritePunctuation(")");
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
        }

        private static void WriteDoWhileStatement(BoundDoWhileStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("do ");
            writer.WriteLine();
            node.Body.WriteTo(writer);
            writer.WriteLine();
            writer.WriteKeyword("while ");
            writer.WritePunctuation("(");
            node.Condition.WriteTo(writer);
            writer.WritePunctuation(")");
        }

        private static void WriteForStatement(BoundForStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("for ");
            writer.WritePunctuation("(");
            writer.WriteIdentifier(node.Variable.Name);
            writer.WritePunctuation(" = ");
            node.LowerBound.WriteTo(writer);
            writer.WritePunctuation("..");
            node.UpperBound.WriteTo(writer);
            writer.WritePunctuation(")");
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
        }

        private static void WriteLabel(BoundLabelStatement node, IndentedTextWriter writer)
        {
            bool unindent = writer.Indent > 0;
            if (unindent)
                writer.Indent--;

            writer.WriteLabel($"{node.Label.Name}:");
            writer.WriteLine();

            if (unindent)
                writer.Indent++;
        }

        private static void WriteGotoStatement(BoundGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("go to ");
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteLine();
        }

        private static void WriteConditionalGotoStatement(BoundConditionalGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("go to ");
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteKeyword(node.JumpIfFalse ? " unless " : " if ");
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteExpressionStatement(BoundExpressionStatement node, IndentedTextWriter writer)
        {
            node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteError(BoundErrorExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("ERROR");
        }

        private static void WriteLiteralExpression(BoundLiteralExpression node, IndentedTextWriter writer)
        {
            if (node.Value == null)
            {
                writer.WriteKeyword("null");
                return;
            }
            string? value = node.Value.ToString();
            if (value == null)
            {
                writer.WriteKeyword("null");
                return;
            }
            if (node.Type == TypeSymbol.Bool)
                writer.WriteKeyword(value);
            else if (node.Type == TypeSymbol.Int)
                writer.WriteNumber(value);
            else if (node.Type == TypeSymbol.String)
            {
                value = "\"" + value.Replace("\"", "\"\"") + "\"";
                writer.WriteString(value);
            }
            else
                throw new Exception($"Unexpected type {node.Type}");
        }

        private static void WriteVariableExpression(BoundVariableExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
        }

        private static void WriteAssignmentExpression(BoundAssignmentExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WritePunctuation(" = ");
            node.Expression.WriteTo(writer);
        }

        private static void WriteUnaryExpression(BoundUnaryExpression node, IndentedTextWriter writer)
        {
            int precedence = SyntaxFacts.GetUnaryOperatorPrecedence(node.Operator.SyntaxKind);
            string? operatorText = SyntaxFacts.GetText(node.Operator.SyntaxKind);

            if (operatorText != null)
                writer.WritePunctuation(operatorText);

            writer.WriteNestedExpression(precedence, node.Operand);
        }

        private static void WriteBinaryExpression(BoundBinaryExpression node, IndentedTextWriter writer)
        {
            int precedence = SyntaxFacts.GetBinaryOperatorPrecedence(node.Operator.SyntaxKind);
            string? operatorText = SyntaxFacts.GetText(node.Operator.SyntaxKind);

            writer.WriteNestedExpression(precedence, node.Left);
            writer.Write(" ");
            if (operatorText != null)
                writer.WritePunctuation(operatorText);
            writer.Write(" ");
            writer.WriteNestedExpression(precedence, node.Right);
        }

        private static void WriteCallExpression(BoundCallExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Function.Name);
            writer.WritePunctuation("(");

            bool isFirst = true;
            foreach (BoundExpression argument in node.Arguments)
            {
                if (isFirst)
                    isFirst = false;
                else
                    writer.WritePunctuation(", ");

                argument.WriteTo(writer);
            }

            writer.WritePunctuation(")");
        }

        private static void WriteConversionExpression(BoundConversionExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Type.Name);
            writer.WritePunctuation("(");
            node.Expression.WriteTo(writer);
            writer.WritePunctuation(")");
        }
    }
}