using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundGlobalScope
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<NamespaceSymbol> Namespaces { get; }
        public ImmutableDictionary<NamespaceSymbol, List<NamespaceSymbol>> Usings { get; }

        public BoundGlobalScope(ImmutableArray<Diagnostic> diagnostics, ImmutableArray<NamespaceSymbol> namespaces, ImmutableDictionary<NamespaceSymbol, List<NamespaceSymbol>> usings)
        {
            Diagnostics = diagnostics;
            Namespaces = namespaces;
            Usings = usings;
        }
    }
}