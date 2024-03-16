using Blaze.SyntaxTokens;
using System.Collections.Immutable;

namespace Blaze.Syntax_Nodes
{
    public sealed class BlockStatementSyntax : StatementSyntax
    {
        public SyntaxToken OpenBraceToken { get; private set; }
        public ImmutableArray<StatementSyntax> Statements { get; private set; }
        public SyntaxToken CloseBraceToken { get; private set; }

        public override SyntaxKind Kind => SyntaxKind.BlockStatement;

        internal BlockStatementSyntax(SyntaxTree tree, SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, SyntaxToken closeBraceToken) : base(tree)
        {
            OpenBraceToken = openBraceToken;
            Statements = statements;
            CloseBraceToken = closeBraceToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;
            foreach (StatementSyntax statement in Statements)
                yield return statement;
            yield return CloseBraceToken;
        }
    }
}
