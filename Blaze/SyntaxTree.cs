using Blaze.Diagnostics;
using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Blaze
{
    public sealed class SyntaxTree
    {
        private delegate void ParseHandler(SyntaxTree syntaxTree, out CompilationUnitSyntax? root, out ImmutableArray<Diagnostic> diagnostics);

        public CompilationUnitSyntax Root { get; private set; }
        public SourceText Text { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        private SyntaxTree(SourceText text, ParseHandler handler)
        {
            Text = text;
            handler(this, out CompilationUnitSyntax? root, out ImmutableArray<Diagnostic> diagnostics);
            Debug.Assert(root != null);
            Diagnostics = diagnostics;
            Root = root;
        }

        public static SyntaxTree Load(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var sourceText = SourceText.From(text, fileName);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text, Parse);
        }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));

        
        public static ImmutableArray<SyntaxToken> ParseTokens(string text, bool includeEndOfFile = false) => ParseTokens(text, out _, includeEndOfFile);
        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false) => ParseTokens(SourceText.From(text), out diagnostics, includeEndOfFile);
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, bool includeEndOfFile = false) => ParseTokens(text, out _, includeEndOfFile);
        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
        {
            var tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree syntaxTree, out CompilationUnitSyntax? root, out ImmutableArray<Diagnostic> diags)
            {
                root = null;

                var lexer = new Lexer(syntaxTree);
                while (true)
                {
                    var token = lexer.Lex();

                    if (token.Kind != SyntaxKind.EndOfFileToken || includeEndOfFile)
                        tokens.Add(token);

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        root = new CompilationUnitSyntax(syntaxTree, ImmutableArray<UsingNamespaceSyntax>.Empty, ImmutableArray<NamespaceDeclarationSyntax>.Empty, token);
                        break;
                    }
                        
                }
                diags = lexer.Diagnostics.ToImmutableArray();
            }
            var tree = new SyntaxTree(text, ParseTokens);
            diagnostics = tree.Diagnostics;
            return tokens.ToImmutableArray();
        }

        private static void Parse(SyntaxTree tree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics)
        {
            var parser = new Parser(tree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }
    }
}
