using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<MemberSyntax> Members { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        public CompilationUnitSyntax(SyntaxTree tree, ImmutableArray<MemberSyntax> members, SyntaxToken endOfFileToken) : base(tree)
        {
            Members = members;
            EndOfFileToken = endOfFileToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (MemberSyntax member in Members)
                yield return member;
            yield return EndOfFileToken;
        }
    }

    public abstract class MemberSyntax : SyntaxNode
    {
        public MemberSyntax(SyntaxTree tree) : base(tree) { }
    }
}
