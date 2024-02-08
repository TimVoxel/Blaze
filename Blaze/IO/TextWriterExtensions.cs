using Blaze.Diagnostics;
using Blaze.Text;
using System.CodeDom.Compiler;
using System.Collections.Immutable;

namespace Blaze.IO
{
    public static class TextWriterExtensions
    {
        public static bool IsConsoleOut(this TextWriter writer)
        {
            if (writer == Console.Out)
                return true;

            return writer is IndentedTextWriter iw && iw.InnerWriter == Console.Out;
        }

        public static void WriteDiagnostics(this TextWriter writer, ImmutableArray<Diagnostic> diagnostics, SyntaxTree syntaxTree)
        {
            foreach (Diagnostic diagnostic in diagnostics.OrderBy(d => d.Location.Span.Start).ThenBy(d => d.Location.Span.Length))
            {
                string fileName = diagnostic.Location.FileName;
                TextSpan span = diagnostic.Location.Span;

                int lineIndex = syntaxTree.Text.GetLineIndex(span.Start);
                TextLine line = syntaxTree.Text.Lines[lineIndex];
                int lineNumber = lineIndex + 1;
                int character = span.Start - syntaxTree.Text.Lines[lineIndex].Start + 1;

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{fileName}({lineNumber}, {character}): ");
                Console.WriteLine(diagnostic);
                Console.ResetColor();

                TextSpan prefixSpan = TextSpan.FromBounds(line.Start, span.Start);
                TextSpan suffixSpan = TextSpan.FromBounds(span.End, line.End);

                string prefix = syntaxTree.Text.ToString(prefixSpan);
                string error = syntaxTree.Text.ToString(span);
                string suffix = syntaxTree.Text.ToString(suffixSpan);

                Console.Write("    ");
                Console.Write(prefix);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(error);
                Console.ResetColor();

                Console.Write(suffix);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsoleOut())
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsoleOut())
                Console.ResetColor();
        }

        public static void WriteKeyword(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkCyan);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteIdentifier(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Cyan);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteString(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkYellow);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteNumber(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Yellow);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WritePunctuation(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Gray);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteLabel(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkGray);
            writer.Write(text);
            writer.ResetColor();
        }
    }
}
