using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class ReturnStatementSyntax : StatementSyntax 
    {   
        public SyntaxToken Keyword { get; private set; }
        public ExpressionSyntax? Expression { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.ReturnStatement;

        public ReturnStatementSyntax(SyntaxToken keyword, ExpressionSyntax? expression, SyntaxToken semicolon)
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
