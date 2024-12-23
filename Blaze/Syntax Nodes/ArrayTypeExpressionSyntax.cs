using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class ArrayTypeExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Identifier { get; }
        public SyntaxToken OpenSquareBracket { get; }
        public ImmutableArray<SyntaxToken> RankSpecifiers { get; }
        public SyntaxToken CloseSquareBracket { get; }

        public override SyntaxKind Kind => SyntaxKind.ArrayTypeExpression;

        internal ArrayTypeExpressionSyntax(SyntaxTree tree, ExpressionSyntax identifier, SyntaxToken openSquareBracket, ImmutableArray<SyntaxToken> rankSpecifiers, SyntaxToken closeSquareBracket) : base(tree)
        {
            Identifier = identifier;
            OpenSquareBracket = openSquareBracket;
            RankSpecifiers = rankSpecifiers;
            CloseSquareBracket = closeSquareBracket;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return OpenSquareBracket;
            foreach (var node in RankSpecifiers)
                yield return node;
            yield return CloseSquareBracket;
        }
    }
}
