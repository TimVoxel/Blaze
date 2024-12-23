namespace Blaze.Syntax_Nodes
{
    public class TypeClauseSyntax : SyntaxNode
    {
        public ExpressionSyntax Expression { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.TypeClause;

        internal TypeClauseSyntax(SyntaxTree tree, ExpressionSyntax expression) : base(tree)
        {
            Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }
    }
}
