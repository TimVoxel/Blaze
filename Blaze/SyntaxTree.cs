using Blaze.Diagnostics;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;

namespace Blaze
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

        
        public static ImmutableArray<SyntaxToken> ParseTokens(string text) => ParseTokens(SourceText.From(text), out _);
        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics) => ParseTokens(SourceText.From(text), out diagnostics);
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text) => ParseTokens(text, out _);
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics)
        {
            IEnumerable<SyntaxToken> LexTokens(Lexer lexer)
            {
                while (true)
                {
                    SyntaxToken token = lexer.Lex();
                    if (token.Kind == SyntaxKind.EndOfFileToken)
                        break;
                    yield return token;
                }
            }

            Lexer lexer = new Lexer(text);
            ImmutableArray<SyntaxToken> result = LexTokens(lexer).ToImmutableArray();
            diagnostics = lexer.Diagnostics.ToImmutableArray();
            return result;
        }
    }
}
