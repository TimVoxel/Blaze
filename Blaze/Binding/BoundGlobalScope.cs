using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableArray<VariableSymbol> Variables { get; private set; }
        public ImmutableArray<FunctionSymbol> Functions { get; private set; }
        public ImmutableArray<BoundStatement> Statements { get; private set; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, ImmutableArray<FunctionSymbol> functions, ImmutableArray<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Functions = functions;
            Statements = statements;
        }
    }
}