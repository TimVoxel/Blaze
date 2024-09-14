using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class EnumDeclarationSyntax : DeclarationSyntax
    {
        public SyntaxToken EnumKeyword { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenBrace { get; }
        public ImmutableArray<EnumMemberDeclarationSyntax> MemberDeclarations { get; }
        public SyntaxToken CloseBrace { get; }

        public override SyntaxKind Kind => SyntaxKind.EnumDeclaration;

        public EnumDeclarationSyntax(SyntaxTree tree, SyntaxToken enumKeyword, SyntaxToken identifier, SyntaxToken openBrace, ImmutableArray<EnumMemberDeclarationSyntax> memberDeclarations, SyntaxToken closeBrace) : base(tree)
        {
            EnumKeyword = enumKeyword;
            Identifier = identifier;
            OpenBrace = openBrace;
            MemberDeclarations = memberDeclarations;
            CloseBrace = closeBrace;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return EnumKeyword;
            yield return Identifier;
            yield return OpenBrace;
            foreach (var member in MemberDeclarations)
                yield return member;
            yield return CloseBrace;
        }
    }
}
