using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ForStatementSyntax : StatementSyntax
    {
        public SyntaxToken ForKeyword { get; }
        public SyntaxToken OpenParen { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken EqualsSign { get; }
        public ExpressionSyntax LowerBound { get; }
        public SyntaxToken DoubleDotToken { get;  }
        public ExpressionSyntax UpperBound { get; }
        public SyntaxToken CloseParen { get; }
        public StatementSyntax Body { get; }

        public override SyntaxKind Kind => SyntaxKind.ForStatement;

        public ForStatementSyntax(SyntaxTree tree, SyntaxToken forKeyword, SyntaxToken openParen, SyntaxToken identifier, SyntaxToken equalsSign, ExpressionSyntax lowerBound, SyntaxToken doubleDotToken, ExpressionSyntax upperBound, SyntaxToken closeParen, StatementSyntax body) : base(tree)
        {
            ForKeyword = forKeyword;
            OpenParen = openParen;
            Identifier = identifier;
            EqualsSign = equalsSign;
            LowerBound = lowerBound;
            DoubleDotToken = doubleDotToken;
            UpperBound = upperBound;
            CloseParen = closeParen;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ForKeyword;
            yield return OpenParen;
            yield return Identifier;
            yield return EqualsSign;
            yield return LowerBound;
            yield return DoubleDotToken;
            yield return UpperBound;
            yield return CloseParen;
            yield return Body;
        }
    }
}
