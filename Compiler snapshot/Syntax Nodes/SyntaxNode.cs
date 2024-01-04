namespace Compiler_snapshot.Syntax_Nodes
{
    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }
        public abstract IEnumerable<SyntaxNode> GetChildren();
    }

    public abstract class ExpressionSyntax : SyntaxNode
    {

    }
}
