using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;
using DPP_Compiler.Text;
using System.Collections.Immutable;

namespace DPP_Compiler
{
    public sealed class SyntaxTree
    {
        public CompilationUnitSyntax Root { get; private set; }
        public SourceText Text { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        private SyntaxTree(SourceText text)
        {
            Parser parser = new Parser(text);
            CompilationUnitSyntax root = parser.ParseCompilationUnit();
            ImmutableArray<Diagnostic> diagnostics = parser.Diagnostics.ToImmutableArray();

            Text = text;
            Diagnostics = diagnostics;
            Root = root;
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text);
        }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));

        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            Lexer lexer = new Lexer(SourceText.From(text));
            while (true)
            {
                SyntaxToken token = lexer.Lex();
                if (token.Kind == SyntaxKind.EndOfFileToken)
                    break;
                yield return token;
            }
        }
    }
}
