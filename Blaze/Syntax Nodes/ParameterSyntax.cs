using Blaze.SyntaxTokens;

namespace Blaze.Syntax_Nodes
{
    public sealed class ParameterSyntax : SyntaxNode
    {
        public TypeClauseSyntax Type { get; private set; }
        public SyntaxToken Identifier { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.Parameter;

        internal ParameterSyntax(SyntaxTree tree, TypeClauseSyntax type, SyntaxToken identifier) : base(tree)
        {
            Type = type;
            Identifier = identifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Type;
            yield return Identifier;
        }
    }
}
