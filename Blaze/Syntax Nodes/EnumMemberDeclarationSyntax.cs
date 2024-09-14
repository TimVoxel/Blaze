using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class EnumMemberDeclarationSyntax : DeclarationSyntax
    {
        public SyntaxToken Identifier { get; }
        public EnumMemberEqualsSyntax? EqualsSyntax { get; }
        public SyntaxToken? CommaToken { get; }

        public override SyntaxKind Kind => SyntaxKind.EnumMemberDeclaration;

        public EnumMemberDeclarationSyntax(SyntaxTree tree, SyntaxToken identifier, EnumMemberEqualsSyntax? equalsSyntax, SyntaxToken? commaToken) : base(tree)
        {
            Identifier = identifier;
            EqualsSyntax = equalsSyntax;
            CommaToken = commaToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            if (EqualsSyntax != null)
                yield return EqualsSyntax;
            if (CommaToken != null)
                yield return CommaToken;
        }
    }
}
