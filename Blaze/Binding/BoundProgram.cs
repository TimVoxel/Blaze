using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal class BoundProgram
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableDictionary<NamespaceSymbol, BoundNamespace> Namespaces { get; private set; }
        
        public BoundProgram(ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<NamespaceSymbol, BoundNamespace> namespaces)
        {
            Diagnostics = diagnostics;
            Namespaces = namespaces;
        }
    }
}