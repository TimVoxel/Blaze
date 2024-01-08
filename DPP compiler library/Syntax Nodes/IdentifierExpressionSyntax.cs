using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class IdentifierExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.IdentifierExpression;

        public IdentifierExpressionSyntax(SyntaxToken identifierToken)
        {
            IdentifierToken = identifierToken;
        }
    }
}
