using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal class BoundProgram
    {
        public NamespaceSymbol GlobalNamespace { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<NamespaceSymbol, BoundNamespace> Namespaces { get; }
        
        public BoundProgram(NamespaceSymbol globalNamespace, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<NamespaceSymbol, BoundNamespace> namespaces)
        {
            GlobalNamespace = globalNamespace;
            Diagnostics = diagnostics;
            Namespaces = namespaces;
        }
    }
}