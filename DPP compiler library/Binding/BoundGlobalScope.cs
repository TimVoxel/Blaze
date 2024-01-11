using DPP_Compiler.Diagnostics;
using System.Collections.Immutable;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableArray<VariableSymbol> Variables { get; private set; }
        public BoundStatement Statement { get; private set; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, BoundStatement statement)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Statement = statement;
        }
    }
}