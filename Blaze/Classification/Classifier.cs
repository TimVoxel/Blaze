using Blaze.Syntax_Nodes;
using Blaze.SyntaxTokens;
using Blaze.Text;
using System.Collections.Immutable;

namespace Blaze.Classification
{
    public sealed class Classifier
    {
        public static ImmutableArray<ClassifiedSpan> Classify(SyntaxTree tree, TextSpan span)
        {
            var result = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            ClassifyNode(tree.Root, span, result);
            return result.ToImmutable();
        }

        private static void ClassifyNode(SyntaxNode? node, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (node == null || !node.FullSpan.OverlapsWith(span))
                return;

            if (node is SyntaxToken token)
                ClassifyToken(token, span, result);

            foreach (var child in node.GetChildren())
                ClassifyNode(child, span, result);
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            foreach (var leadingTrivia in token.LeadingTrivia)
                ClassifyTrivia(leadingTrivia, span, result);

            AddClassification(token.Kind, token.Span, span, result);

            foreach (var trailingTrivia in token.TrailingTrivia)
                ClassifyTrivia(trailingTrivia, span, result);
        }

        private static void ClassifyTrivia(Trivia trivia, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            AddClassification(trivia.Kind, trivia.Span, span, result);
        }

        private static void AddClassification(SyntaxKind elementKind, TextSpan elementSpan, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (!elementSpan.OverlapsWith(span))
                return;

            var adjustedStart = Math.Max(elementSpan.Start, span.Start);
            var adjustedEnd = Math.Min(elementSpan.End, span.End);
            var adjustedSpan = TextSpan.FromBounds(adjustedStart, adjustedEnd);
            var classification = GetClassification(elementKind);
            var classifiedSpan = new ClassifiedSpan(adjustedSpan, classification);
            result.Add(classifiedSpan);
        }

        private static Classification GetClassification(SyntaxKind kind) => kind switch
        {
            SyntaxKind.IntegerLiteralToken => Classification.Number,
            SyntaxKind.IdentifierToken => Classification.Identifier,
            SyntaxKind.StringLiteralToken => Classification.String,
            _ when SyntaxFacts.IsComment(kind) => Classification.Comment,
            _ when SyntaxFacts.IsKeyword(kind) => Classification.Keyword,
            _ => Classification.Text
        };
    }
}
