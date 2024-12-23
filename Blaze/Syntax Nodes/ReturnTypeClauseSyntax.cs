using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ReturnTypeClauseSyntax : TypeClauseSyntax
    {
        public SyntaxToken ColonToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ReturnTypeClause;

        internal ReturnTypeClauseSyntax(SyntaxTree tree, SyntaxToken colonToken, ExpressionSyntax expression) : base(tree, expression)
        {
            ColonToken = colonToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ColonToken;
            yield return Expression;
        }
    }
}
