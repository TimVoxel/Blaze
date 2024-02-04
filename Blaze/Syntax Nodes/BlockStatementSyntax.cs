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

        public BlockStatementSyntax(SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, SyntaxToken closeBraceToken)
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
