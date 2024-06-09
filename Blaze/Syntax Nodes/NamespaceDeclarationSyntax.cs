using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class NamespaceDeclarationSyntax : MemberSyntax
    {
        public SyntaxToken NamespaceKeyword { get; }
        public SeparatedSyntaxList<SyntaxToken> IdentifierPath { get; }
        public SyntaxToken OpenBraceToken { get; }
        public ImmutableArray<MemberSyntax> Members { get; } 
        public SyntaxToken CloseBraceToken { get; }

        public override SyntaxKind Kind => SyntaxKind.NamespaceDeclaration;

        public NamespaceDeclarationSyntax(SyntaxTree tree, SyntaxToken namespaceKeyword, SeparatedSyntaxList<SyntaxToken> identifierPath, SyntaxToken openBraceToken, ImmutableArray<MemberSyntax> members, SyntaxToken closeBraceToken) : base(tree)
        {
            NamespaceKeyword = namespaceKeyword;
            IdentifierPath = identifierPath;
            OpenBraceToken = openBraceToken;
            Members = members;
            CloseBraceToken = closeBraceToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NamespaceKeyword;
            foreach (SyntaxNode node in IdentifierPath.GetWithSeparators())
                yield return node;
            yield return OpenBraceToken;
            foreach (SyntaxNode node in Members)
                yield return node;
            yield return CloseBraceToken;
        }
    }
}
