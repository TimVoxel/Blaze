using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public StatementSyntax Statement { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        public CompilationUnitSyntax(StatementSyntax expression, SyntaxToken endOfFileToken)
        {
            Statement = expression;
            EndOfFileToken = endOfFileToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Statement;
            yield return EndOfFileToken;
        }
    }
}
