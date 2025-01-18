using Blaze.Diagnostics;
using Blaze.Text;
using System.CodeDom.Compiler;
using System.Collections.Immutable;

namespace Blaze.IO
{
    public static class DirectoryExtensions
    {
        public static void Copy(string sourceDir, string destinationDir, bool recursive = true)
        {
            var directory = new DirectoryInfo(sourceDir);

            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {directory.FullName}");

            var dirs = directory.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (var file in directory.GetFiles())
            {
                var targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (var subDir in dirs)
                {
                    var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    Copy(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }

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
            foreach (var diagnostic in diagnostics.Where(d => d.Location.Text == null))
            {
                writer.SetForeground(ConsoleColor.Red);
                writer.Write(diagnostic.Message);
                writer.WriteLine(diagnostic);
                writer.ResetColor();
            } 

            foreach (var diagnostic in diagnostics.Where(d => d.Location.Text != null).
                                                          OrderBy(d => d.Location.FileName).
                                                          ThenBy(d => d.Location.Span.Start).
                                                          ThenBy(d => d.Location.Span.Length))
            {
                var text = diagnostic.Location.Text;
                var fileName = diagnostic.Location.FileName;
                var source = diagnostic.Source.DiagnosticsSourceName;
                var startLine = diagnostic.Location.StartLine + 1;
                var startCharacter = diagnostic.Location.StartCharacter + 1;
                var endLine = diagnostic.Location.EndLine + 1;
                var endCharacter = diagnostic.Location.EndCharacter + 1;
                var span = diagnostic.Location.Span;

                var lineIndex = text.GetLineIndex(span.Start);
                var line = text.Lines[lineIndex];
                var lineNumber = lineIndex + 1;
                var character = span.Start - text.Lines[lineIndex].Start + 1;

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{source}: {fileName}(from {startLine},{startCharacter} to {endLine},{endCharacter}): ");
                Console.WriteLine(diagnostic);
                Console.ResetColor();

                Console.Write("    ");

                if (startLine == endLine)
                {
                    var prefixSpan = TextSpan.FromBounds(line.Start, span.Start);
                    var suffixSpan = TextSpan.FromBounds(span.End, line.End);
                    var prefix = text.ToString(prefixSpan);
                    var error = text.ToString(span);
                    var suffix = text.ToString(suffixSpan);
                    
                    Console.Write(prefix);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(error);
                    Console.ResetColor();

                    Console.Write(suffix);
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{line}...");
                }
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

        public static void WriteTrivia(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Green);
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
