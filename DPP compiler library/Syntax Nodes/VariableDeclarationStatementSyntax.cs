using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class VariableDeclarationStatementSyntax : StatementSyntax {

        public SyntaxToken DeclarationToken { get; private set; }
        public SyntaxToken Identifier { get; private set; }
        public SyntaxToken EqualsToken { get; private set; }
        public ExpressionSyntax Initializer { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        public VariableDeclarationStatementSyntax(SyntaxToken declarationToken, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax initializer, SyntaxToken semicolon)
        {
            DeclarationToken = declarationToken;
            Identifier = identifier;
            EqualsToken = equalsToken;
            Initializer = initializer;
            Semicolon = semicolon;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DeclarationToken;
            yield return Identifier;
            yield return EqualsToken;
            yield return Initializer;
            yield return Semicolon;
        }
    }
}
