using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ForStatementSyntax : StatementSyntax
    {
        public SyntaxToken ForKeyword { get; private set; }
        public SyntaxToken Identifier { get; private set; }
        public SyntaxToken EqualsSign { get; private set; }
        public ExpressionSyntax LowerBound { get; private set; }
        public SyntaxToken DoubleDotToken { get; private set; }
        public ExpressionSyntax UpperBound { get; private set; }
        public StatementSyntax Body { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ForStatement;

        public ForStatementSyntax(SyntaxToken forKeyword, SyntaxToken identifier, SyntaxToken equalsSign, ExpressionSyntax lowerBound, SyntaxToken doubleDotToken, ExpressionSyntax upperBound, StatementSyntax body)
        {
            ForKeyword = forKeyword;
            Identifier = identifier;
            EqualsSign = equalsSign;
            LowerBound = lowerBound;
            DoubleDotToken = doubleDotToken;
            UpperBound = upperBound;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ForKeyword;
            yield return Identifier;
            yield return EqualsSign;
            yield return LowerBound;
            yield return DoubleDotToken;
            yield return UpperBound;
            yield return Body;
        }
    }
}
