using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ObjectCreationExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Keyword { get; }
        public ExpressionSyntax Identifier { get; }
        public SyntaxToken OpenParen { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParen { get; }

        public override SyntaxKind Kind => SyntaxKind.ObjectCreationExpression;

        internal ObjectCreationExpressionSyntax(SyntaxTree tree, SyntaxToken keyword, ExpressionSyntax identifier, SyntaxToken openParen, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParen) : base(tree)
        {
            Keyword = keyword;
            Identifier = identifier;
            OpenParen = openParen;
            Arguments = arguments;
            CloseParen = closeParen;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            yield return Identifier;
            yield return OpenParen;
            foreach (var node in Arguments.GetWithSeparators())
                yield return node;
            yield return CloseParen;
        }
    }
}
