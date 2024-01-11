using DPP_Compiler.SyntaxTokens;

namespace DPP_Compiler.Syntax_Nodes
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public ExpressionSyntax Expression { get; private set; }
        public SyntaxToken EndOfFileToken { get; private set; }

        public CompilationUnitSyntax(ExpressionSyntax expression, SyntaxToken endOfFileToken)
        {
            Expression = expression;
            EndOfFileToken = endOfFileToken;
        }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return EndOfFileToken;
        }
    }
}
