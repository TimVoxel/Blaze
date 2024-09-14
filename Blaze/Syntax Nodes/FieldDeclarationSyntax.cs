using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class FieldDeclarationSyntax : DeclarationSyntax
    {
        public SyntaxNode DeclarationNode { get; }
        public SyntaxToken Identifier { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Initializer { get; }
        public SyntaxToken Semicolon { get; }

        public override SyntaxKind Kind => SyntaxKind.FieldDeclaration;

        internal FieldDeclarationSyntax(SyntaxTree tree, SyntaxNode declarationNode, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax initializer, SyntaxToken semicolon) : base(tree)
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
