using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ReturnTypeClauseSyntax : SyntaxNode
    {
        public SyntaxToken ColonToken { get; private set; }
        public SyntaxToken Identifier { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ReturnTypeClause;

        public ReturnTypeClauseSyntax(SyntaxTree tree, SyntaxToken colonToken, SyntaxToken identifier) : base(tree)
        {
            ColonToken = colonToken;
            Identifier = identifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ColonToken;
            yield return Identifier;
        }
    }
}
