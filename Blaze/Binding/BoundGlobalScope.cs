using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundGlobalScope
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public NamespaceSymbol GlobalNamespace { get; }

        public BoundGlobalScope(ImmutableArray<Diagnostic> diagnostics, NamespaceSymbol globalNamespace)
        {
            Diagnostics = diagnostics;
            GlobalNamespace = globalNamespace;
        }
    }
}