using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class WhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken KeywordToken { get; }
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax Condition { get; }
        public SyntaxToken CloseParenToken { get; }
        public StatementSyntax Body { get; }

        public override SyntaxKind Kind => SyntaxKind.WhileStatement;

        internal WhileStatementSyntax(SyntaxTree tree, SyntaxToken keywordToken, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax body) : base(tree)
        {
            KeywordToken = keywordToken;
            OpenParenToken = openParenToken;
            Condition = condition;
            CloseParenToken = closeParenToken;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return KeywordToken;
            yield return OpenParenToken;
            yield return Condition;
            yield return CloseParenToken;
            yield return Body;
        }
    }
}