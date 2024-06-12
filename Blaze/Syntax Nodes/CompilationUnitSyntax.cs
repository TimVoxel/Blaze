using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<UsingDirectiveSyntax> Usings { get; }
        public ImmutableArray<NamespaceDeclarationSyntax> Namespaces { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        internal CompilationUnitSyntax(SyntaxTree tree, ImmutableArray<UsingDirectiveSyntax> usings, ImmutableArray<NamespaceDeclarationSyntax> namespaces, SyntaxToken endOfFileToken) : base(tree)
        {
            Usings = usings;
            Namespaces = namespaces;
            EndOfFileToken = endOfFileToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var member in Usings)
                yield return member;
            foreach (var member in Namespaces)
                yield return member;
            yield return EndOfFileToken;
        }
    }
}
