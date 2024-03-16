using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class TypeClauseSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.TypeClause;

        internal TypeClauseSyntax(SyntaxTree tree, SyntaxToken identifier) : base(tree)
        {
            Identifier = identifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
        }
    }
}
