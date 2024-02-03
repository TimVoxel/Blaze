using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class BreakStatementSyntax : StatementSyntax
    {
        public SyntaxToken Keyword { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.BreakStatement;

        public BreakStatementSyntax(SyntaxToken keyword, SyntaxToken semicolon)
        {
            Keyword = keyword;
            Semicolon = semicolon;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            yield return Semicolon;
        }
    }
}