using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class UsingDirectiveSyntax : SyntaxNode
    {
        public SyntaxToken UsingKeyword { get; }
        public SeparatedSyntaxList<SyntaxToken> IdentifierPath { get; }
        public SyntaxToken Semicolon { get; }

        public override SyntaxKind Kind => SyntaxKind.UsingNamespace;

        public UsingDirectiveSyntax(SyntaxTree tree, SyntaxToken keyword, SeparatedSyntaxList<SyntaxToken> identifierPath, SyntaxToken semicolon) : base(tree)
        {
            UsingKeyword = keyword;
            IdentifierPath = identifierPath;
            Semicolon = semicolon;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return UsingKeyword;
            foreach (var node in IdentifierPath.GetWithSeparators())
                yield return node;
            yield return Semicolon;
        }
    }
}
