using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class DoWhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken DoKeyword { get; private set; }
        public StatementSyntax Body { get; private set; }
        public SyntaxToken WhileKeyword { get; private set; }
        public SyntaxToken OpenParen { get; private set; }
        public ExpressionSyntax Condition { get; private set; }
        public SyntaxToken CloseParen { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.DoWhileStatement;

        public DoWhileStatementSyntax(SyntaxTree tree, SyntaxToken doKeyword, StatementSyntax body, SyntaxToken whileKeyword, SyntaxToken openParen, ExpressionSyntax condition, SyntaxToken closeParen, SyntaxToken semicolon) : base(tree)
        {
            DoKeyword = doKeyword;
            Body = body;
            WhileKeyword = whileKeyword;
            OpenParen = openParen;
            Condition = condition;
            CloseParen = closeParen;
            Semicolon = semicolon;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DoKeyword;
            yield return Body;
            yield return WhileKeyword;
            yield return OpenParen;
            yield return Condition;
            yield return CloseParen;
            yield return Semicolon;
        }
    }
}
