using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ArrayAccessExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Identifier { get; }
        public SyntaxToken OpenSquareBracketToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseSquareBracketToken { get; }

        public override SyntaxKind Kind => SyntaxKind.ArrayAccessExpression;

        public ArrayAccessExpressionSyntax(SyntaxTree tree, ExpressionSyntax identifier, SyntaxToken openSquareBracketToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeSquareBracketToken) : base(tree)
        {
            Identifier = identifier;
            OpenSquareBracketToken = openSquareBracketToken;
            Arguments = arguments;
            CloseSquareBracketToken = closeSquareBracketToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return OpenSquareBracketToken;
            foreach (var node in Arguments.GetWithSeparators())
                yield return node;
            yield return CloseSquareBracketToken;
        }
    }
}
