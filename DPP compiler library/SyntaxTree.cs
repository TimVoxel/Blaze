using DPP_Compiler.Diagnostics;
using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;
using System.ComponentModel.DataAnnotations;

namespace DPP_Compiler
{
    public sealed class SyntaxTree
    {
        public ExpressionSyntax Root { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; private set; }

        public SyntaxTree(IEnumerable<Diagnostic> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
        {
            Diagnostics = diagnostics.ToArray();
            Root = root;
            EndOfFileToken = endOfFileToken;
        }

        public static SyntaxTree Parse(string text)
        {
            Parser parser = new Parser(text);
            return parser.Parse();
        }

        public static IEnumerable<SyntaxToken> ParseTokens(string text)
        {
            Lexer lexer = new Lexer(text);
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
