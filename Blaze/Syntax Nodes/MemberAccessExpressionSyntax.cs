using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax AccessedExpression { get; }
        public SyntaxToken DotToken { get; }
        public SyntaxToken MemberIdentifier { get; }

        public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;

        public MemberAccessExpressionSyntax(SyntaxTree tree, ExpressionSyntax identifier, SyntaxToken dot, SyntaxToken memberIdentifier) : base(tree)
        {
            AccessedExpression = identifier;
            DotToken = dot;
            MemberIdentifier = memberIdentifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return AccessedExpression;
            yield return DotToken;
            yield return MemberIdentifier;
        }
    }
}
