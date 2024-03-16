using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ExpressionStatementSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; private set; }
        public SyntaxToken SemicolonToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

        internal ExpressionStatementSyntax(SyntaxTree tree, ExpressionSyntax expression, SyntaxToken semicolonToken) : base(tree)
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
