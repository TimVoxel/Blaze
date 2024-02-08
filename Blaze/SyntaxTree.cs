using Blaze.Diagnostics;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;

namespace Blaze
{
    public sealed class SyntaxTree
    {
        private delegate void ParseHandler(SyntaxTree syntaxTree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics);

        public CompilationUnitSyntax Root { get; private set; }
        public SourceText Text { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        private SyntaxTree(SourceText text, ParseHandler handler)
        {
            Text = text;
            handler(this, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics);
            Diagnostics = diagnostics;
            Root = root;
        }

        public static SyntaxTree Load(string fileName)
        {
            string text = File.ReadAllText(fileName);
            SourceText sourceText = SourceText.From(text, fileName);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text, Parse);
        }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));

        
        public static ImmutableArray<SyntaxToken> ParseTokens(string text) => ParseTokens(SourceText.From(text), out _);
        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics) => ParseTokens(SourceText.From(text), out diagnostics);
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text) => ParseTokens(text, out _);
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics)
        {
            List<SyntaxToken> tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree syntaxTree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diags)
            {
                Lexer lexer = new Lexer(syntaxTree);
                while (true)
                {
                    SyntaxToken token = lexer.Lex();
                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        root = new CompilationUnitSyntax(syntaxTree, ImmutableArray<MemberSyntax>.Empty, token);
                        break;
                    }
                    tokens.Add(token);
                }
                diags = lexer.Diagnostics.ToImmutableArray();
                
            }
            SyntaxTree tree = new SyntaxTree(text, ParseTokens);
            diagnostics = tree.Diagnostics;
            return tokens.ToImmutableArray();
        }

        private static void Parse(SyntaxTree tree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics)
        {
            Parser parser = new Parser(tree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }
    }
}
