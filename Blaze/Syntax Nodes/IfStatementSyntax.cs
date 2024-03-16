using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class IfStatementSyntax : StatementSyntax 
    {
        public SyntaxToken IfKeyword { get; private set; }
        public ExpressionSyntax Condition { get; private set; }
        public StatementSyntax Body { get; private set; }
        public SyntaxToken OpenParen { get; }
        public SyntaxToken CloseParen { get; }
        public ElseClauseSyntax? ElseClause { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IfStatement;

        internal IfStatementSyntax(SyntaxTree tree, SyntaxToken ifKeyword, SyntaxToken openParen, ExpressionSyntax condition, SyntaxToken closeParen, StatementSyntax body, ElseClauseSyntax elseClause) : this(tree, ifKeyword, openParen, condition, closeParen, body)
        {
            ElseClause = elseClause;
        }

        public IfStatementSyntax(SyntaxTree tree, SyntaxToken ifKeyword, SyntaxToken openParen, ExpressionSyntax condition, SyntaxToken closeParen, StatementSyntax body) : base(tree)
        {
            IfKeyword = ifKeyword;
            OpenParen = openParen;
            Condition = condition;
            CloseParen = closeParen;
            Body = body;
        } 

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IfKeyword;
            yield return OpenParen;
            yield return Condition;
            yield return CloseParen;
            yield return Body;
            if (ElseClause != null)
                yield return ElseClause; 
        }
    }

    public sealed class ElseClauseSyntax : SyntaxNode
    {
        public SyntaxToken ElseKeyword { get; private set; }
        public StatementSyntax Body { get; }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;

        public ElseClauseSyntax(SyntaxTree tree, SyntaxToken elseKeyword, StatementSyntax body) : base(tree)
        {
            ElseKeyword = elseKeyword;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ElseKeyword;
            yield return Body;
        }
    }
}
