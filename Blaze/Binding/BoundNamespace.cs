using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal class BoundNamespace : BoundNode
    {
        public NamespaceSymbol Namespace { get; }
        public ImmutableDictionary<NamespaceSymbol, BoundNamespace> Children { get; }
        public ImmutableDictionary<FunctionSymbol, BoundStatement> Functions { get; }
        
        public override BoundNodeKind Kind => BoundNodeKind.Namespace;

        public BoundNamespace(NamespaceSymbol ns, ImmutableDictionary<NamespaceSymbol, BoundNamespace> children, ImmutableDictionary<FunctionSymbol, BoundStatement> functions)
        {
            Children = children;
            Functions = functions;
            Namespace = ns;
        }
    }
}
