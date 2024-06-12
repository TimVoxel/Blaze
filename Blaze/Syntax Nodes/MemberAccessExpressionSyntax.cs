using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Identifier { get; }
        public SyntaxToken DotToken { get; }
        public ExpressionSyntax Member { get; }

        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;

        public MemberAccessExpressionSyntax(SyntaxTree tree, SyntaxToken identifier, SyntaxToken dot, ExpressionSyntax memberIdentifier) : base(tree)
        {
            Identifier = identifier;
            DotToken = dot;
            Member = memberIdentifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return DotToken;
            yield return Member;
        }
    }
}
