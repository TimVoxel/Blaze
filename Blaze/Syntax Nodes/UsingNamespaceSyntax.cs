using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class UsingNamespaceSyntax : SyntaxNode
    {
        public SyntaxToken UsingKeyword { get; }
        public SeparatedSyntaxList<SyntaxToken> IdentifierPath { get; }

        public override SyntaxKind Kind => SyntaxKind.UsingNamespace;

        public UsingNamespaceSyntax(SyntaxTree tree, SyntaxToken keyword, SeparatedSyntaxList<SyntaxToken> identifierPath) : base(tree)
        {
            UsingKeyword = keyword;
            IdentifierPath = identifierPath;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return UsingKeyword;
            foreach (var node in IdentifierPath.GetWithSeparators())
                yield return node;
        }
    }
}
