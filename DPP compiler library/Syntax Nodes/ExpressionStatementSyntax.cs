using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class ExpressionStatementSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; private set; }
        public SyntaxToken SemicolonToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

        public ExpressionStatementSyntax(ExpressionSyntax expression, SyntaxToken semicolonToken)
        {
            Expression = expression;
            SemicolonToken = semicolonToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return SemicolonToken;
        }
    }
}
