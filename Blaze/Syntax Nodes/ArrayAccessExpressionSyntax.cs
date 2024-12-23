using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ArrayAccessExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Identifier { get; }
        public SyntaxToken OpenSquareBracketToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> IndexExpressions { get; }
        public SyntaxToken CloseSquareBracketToken { get; }

        public override SyntaxKind Kind => SyntaxKind.ArrayAccessExpression;

        public ArrayAccessExpressionSyntax(SyntaxTree tree, ExpressionSyntax identifier, SyntaxToken openSquareBracketToken, SeparatedSyntaxList<ExpressionSyntax> indexExpressions, SyntaxToken closeSquareBracketToken) : base(tree)
        {
            Identifier = identifier;
            OpenSquareBracketToken = openSquareBracketToken;
            IndexExpressions = indexExpressions;
            CloseSquareBracketToken = closeSquareBracketToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return OpenSquareBracketToken;
            foreach (var node in IndexExpressions.GetWithSeparators())
                yield return node;
            yield return CloseSquareBracketToken;
        }
    }
}
