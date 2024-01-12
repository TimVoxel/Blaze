using DPP_Compiler.Syntax_Nodes;
using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler
{
    public sealed class WhileStatementSyntax : StatementSyntax
    {
        public SyntaxToken KeywordToken { get; private set; }
        public ExpressionSyntax Condition { get; private set; }
        public StatementSyntax Body { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.WhileStatement;

        public WhileStatementSyntax(SyntaxToken keywordToken, ExpressionSyntax condition, StatementSyntax body)
        {
            KeywordToken = keywordToken;
            Condition = condition;
            Body = body;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return KeywordToken;
            yield return Condition;
            yield return Body;
        }
    }
}
