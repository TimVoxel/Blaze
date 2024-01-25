using DPP_Compiler.SyntaxTokens;
using System.Collections.Immutable;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public ImmutableArray<MemberSyntax> Members { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        public CompilationUnitSyntax(ImmutableArray<MemberSyntax> members, SyntaxToken endOfFileToken)
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

    }
}
