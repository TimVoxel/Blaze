using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundBlockStatement : BoundStatement
    {
        public ImmutableArray<BoundStatement> Statements { get; private set;  }

        public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;

        public BoundBlockStatement(ImmutableArray<BoundStatement> statements)
        {
            Statements = statements;
        }
    }
}