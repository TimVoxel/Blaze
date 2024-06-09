using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundGlobalScope
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<NamespaceSymbol> Namespaces { get; }

        public BoundGlobalScope(ImmutableArray<Diagnostic> diagnostics, ImmutableArray<NamespaceSymbol> namespaces)
        {
            Diagnostics = diagnostics;
            Namespaces = namespaces;
        }
    }
}