using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class IfStatementSyntax : StatementSyntax 
    {
        public SyntaxToken IfKeyword { get; private set; }
        public ExpressionSyntax Condition { get; private set; }
        public StatementSyntax Body { get; private set; }
        public ElseClauseSyntax? ElseClause { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IfStatement;

        public IfStatementSyntax(SyntaxToken ifKeyword, ExpressionSyntax condition, StatementSyntax body, ElseClauseSyntax elseClause) : this(ifKeyword, condition, body)
        {
            ElseClause = elseClause;
        }

        public IfStatementSyntax(SyntaxToken ifKeyword, ExpressionSyntax condition, StatementSyntax body)
        {
            IfKeyword = ifKeyword;
            Condition = condition;
            Body = body;
        } 

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IfKeyword;
            yield return Condition;
            yield return Body;
            if (ElseClause != null)
            {
                foreach (SyntaxNode elseClauseChild in ElseClause.GetChildren())
                    yield return elseClauseChild;
            }       
        }
    }

    public sealed class ElseClauseSyntax : SyntaxNode
    {
        public SyntaxToken ElseKeyword { get; private set; }
        public StatementSyntax Body { get; }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;

        public ElseClauseSyntax(SyntaxToken elseKeyword, StatementSyntax body)
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
