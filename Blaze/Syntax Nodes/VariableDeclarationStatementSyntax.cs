using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class VariableDeclarationStatementSyntax : StatementSyntax {

        public SyntaxNode DeclarationNode { get; private set; }
        public SyntaxToken Identifier { get; private set; }
        public SyntaxToken EqualsToken { get; private set; }
        public ExpressionSyntax Initializer { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        internal VariableDeclarationStatementSyntax(SyntaxTree tree, SyntaxNode declarationNode, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax initializer, SyntaxToken semicolon) : base(tree)
        {
            DeclarationNode = declarationNode;
            Identifier = identifier;
            EqualsToken = equalsToken;
            Initializer = initializer;
            Semicolon = semicolon;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DeclarationNode;
            yield return Identifier;
            yield return EqualsToken;
            yield return Initializer;
            yield return Semicolon;
        }
    }
}
