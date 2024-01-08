using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }
        public SyntaxToken EqualsToken { get; private set; }
        public ExpressionSyntax Expression { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

        public AssignmentExpressionSyntax(SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}
