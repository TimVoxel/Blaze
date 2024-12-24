using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class ArrayCreationExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Keyword { get; }
        public ExpressionSyntax Identifier { get; }
        public SyntaxToken OpenSquareBracket { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseSquareBracket { get; }

        public override SyntaxKind Kind => SyntaxKind.ArrayCreationExpression;

        internal ArrayCreationExpressionSyntax(SyntaxTree tree, SyntaxToken keyword, ExpressionSyntax identifier, SyntaxToken openSquareBracket, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeSquareBracket) : base(tree)
        {
            Keyword = keyword;
            Identifier = identifier;
            OpenSquareBracket = openSquareBracket;
            Arguments = arguments;
            CloseSquareBracket = closeSquareBracket;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            yield return Identifier;
            yield return OpenSquareBracket;
            foreach (var node in Arguments.GetWithSeparators())
                yield return node;
            yield return CloseSquareBracket;
        }
    }
}
