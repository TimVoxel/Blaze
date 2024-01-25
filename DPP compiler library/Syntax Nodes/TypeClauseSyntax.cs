using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class TypeClauseSyntax : SyntaxNode
    {
        public SyntaxToken Identifier { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.TypeClause;

        public TypeClauseSyntax(SyntaxToken identifier)
        {
            Identifier = identifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
        }
    }
}
