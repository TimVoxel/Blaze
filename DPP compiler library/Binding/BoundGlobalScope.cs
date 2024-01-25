using DPP_Compiler.Diagnostics;
using DPP_Compiler.Symbols;
using System.Collections.Immutable;

namespace DPP_Compiler.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableArray<VariableSymbol> Variables { get; private set; }
        public ImmutableArray<FunctionSymbol> Functions { get; private set; }
        public BoundStatement Statement { get; private set; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, ImmutableArray<FunctionSymbol> functions, BoundStatement statement)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Functions = functions;
            Statement = statement;
        }
    }
}