using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class EnumMemberEqualsSyntax : SyntaxNode
    {
        public SyntaxToken EqualsToken { get; }
        public SyntaxToken IntegerLiteral { get; }

        public override SyntaxKind Kind => SyntaxKind.EnumMemberEquals;

        public EnumMemberEqualsSyntax(SyntaxTree tree, SyntaxToken equalsToken, SyntaxToken integerLiteral) : base(tree)
        {
            EqualsToken = equalsToken;
            IntegerLiteral = integerLiteral;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return EqualsToken;
            yield return IntegerLiteral;
        }
    }
}
