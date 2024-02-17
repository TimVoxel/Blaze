using Blaze.Diagnostics;
using Blaze.Text;
using System.CodeDom.Compiler;
using System.Collections.Immutable;

namespace Blaze.IO
{
    public static class TextWriterExtensions
    {
        public static bool IsConsole(this TextWriter writer)
        {
            if (writer == Console.Out)
                return true;

            if (writer == Console.Error)
                return !Console.IsErrorRedirected && !Console.IsOutputRedirected;

            return writer is IndentedTextWriter iw && iw.InnerWriter.IsConsole();
        }

        public static void WriteDiagnostics(this TextWriter writer, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Location.Text == null))
            {
                writer.SetForeground(ConsoleColor.Red);
                writer.Write(diagnostic.Message);
                writer.WriteLine(diagnostic);
                writer.ResetColor();
            } 

            foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Location.Text != null).
                                                          OrderBy(d => d.Location.FileName).
                                                          ThenBy(d => d.Location.Span.Start).
                                                          ThenBy(d => d.Location.Span.Length))
            {
                SourceText text = diagnostic.Location.Text;
                string fileName = diagnostic.Location.FileName;
                int startLine = diagnostic.Location.StartLine + 1;
                int startCharacter = diagnostic.Location.StartCharacter + 1;
                int endLine = diagnostic.Location.EndLine + 1;
                int endCharacter = diagnostic.Location.EndCharacter + 1;
                TextSpan span = diagnostic.Location.Span;

                int lineIndex = text.GetLineIndex(span.Start);
                TextLine line = text.Lines[lineIndex];
                int lineNumber = lineIndex + 1;
                int character = span.Start - text.Lines[lineIndex].Start + 1;

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{fileName}({startLine},{startCharacter},{endLine},{endCharacter}): ");
                Console.WriteLine(diagnostic);
                Console.ResetColor();

                TextSpan prefixSpan = TextSpan.FromBounds(line.Start, span.Start);
                TextSpan suffixSpan = TextSpan.FromBounds(span.End, line.End);

                string prefix = text.ToString(prefixSpan);
                string error = text.ToString(span);
                string suffix = text.ToString(suffixSpan);

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
            if (writer.IsConsole())
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsole())
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
