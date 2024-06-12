using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<UsingNamespaceSyntax> Usings { get; }
        public ImmutableArray<NamespaceDeclarationSyntax> Namespaces { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        internal CompilationUnitSyntax(SyntaxTree tree, ImmutableArray<UsingNamespaceSyntax> usings, ImmutableArray<NamespaceDeclarationSyntax> namespaces, SyntaxToken endOfFileToken) : base(tree)
        {
            Usings = usings;
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
