using Blaze.Symbols;

namespace Blaze.Binding
{
    internal sealed class BoundNamespaceExpression : BoundExpression
    {
        public NamespaceSymbol Namespace { get; }

        public override BoundNodeKind Kind => BoundNodeKind.NamespaceExpression;
        public override TypeSymbol Type => TypeSymbol.Void;

        public BoundNamespaceExpression(NamespaceSymbol namespaceSymbol)
        {
            Namespace = namespaceSymbol;
        }
    }

}