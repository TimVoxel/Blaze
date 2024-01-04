using Compiler_snapshot.Syntax_Nodes;
using Compiler_snapshot.SyntaxTokens;

namespace Compiler_snapshot
{
    public sealed class SyntaxTree
    {
        public ExpressionSyntax Root { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }
        public IReadOnlyList<string> Diagnostics { get; private set; }

        public SyntaxTree(IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
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
    }
}
