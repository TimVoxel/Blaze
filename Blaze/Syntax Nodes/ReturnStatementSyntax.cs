using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ReturnStatementSyntax : StatementSyntax 
    {   
        public SyntaxToken Keyword { get; private set; }
        public ExpressionSyntax? Expression { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ReturnStatement;

        internal ReturnStatementSyntax(SyntaxTree tree, SyntaxToken keyword, ExpressionSyntax? expression, SyntaxToken semicolon) : base(tree)
        {
            Keyword = keyword;
            Expression = expression;
            Semicolon = semicolon;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            if (Expression != null)
                yield return Expression;
            yield return Semicolon;
        }
    }
}
