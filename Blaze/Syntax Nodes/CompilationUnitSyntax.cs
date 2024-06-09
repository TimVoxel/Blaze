using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<NamespaceDeclarationSyntax> Namespaces { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        internal CompilationUnitSyntax(SyntaxTree tree, ImmutableArray<NamespaceDeclarationSyntax> namespaces, SyntaxToken endOfFileToken) : base(tree)
        {
            Namespaces = namespaces;
            EndOfFileToken = endOfFileToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (MemberSyntax member in Namespaces)
                yield return member;
            yield return EndOfFileToken;
        }
    }
}
