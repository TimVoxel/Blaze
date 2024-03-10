using Blaze.Syntax_Nodes;
using Blaze.Text;
using System.Collections.Immutable;

namespace Blaze.SyntaxTokens
{
    public sealed class SyntaxToken : SyntaxNode
    {
        public override SyntaxKind Kind { get; }
        public int Position { get; private set; }
        public string Text { get; private set; }
        public object? Value { get; private set; }

        public ImmutableArray<Trivia> LeadingTrivia { get; private set; }
        public ImmutableArray<Trivia> TrailingTrivia { get; private set; } 

        public override TextSpan Span => new TextSpan(Position, Text.Length);

        public override TextSpan FullSpan
        {
            get
            {
                int startSpan = LeadingTrivia.Length == 0 ? Position : LeadingTrivia.First().Span.Start;
                int endSpan = TrailingTrivia.Length == 0 ? Position : TrailingTrivia.Last().Span.End;
                return TextSpan.FromBounds(startSpan, endSpan);
            }
        }

        public bool IsMissingText => string.IsNullOrEmpty(Text);

        public SyntaxToken(SyntaxTree tree, SyntaxKind kind, int position, string? text, object? value, ImmutableArray<Trivia> leadingTrivia, ImmutableArray<Trivia> trailingTrivia) : base(tree)
        {
            Kind = kind;
            Position = position;
            Text = text ?? string.Empty;
            Value = value;
            LeadingTrivia = leadingTrivia;
            TrailingTrivia = trailingTrivia;
        }

        public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();
    }
}
