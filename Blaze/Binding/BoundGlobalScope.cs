using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal sealed class BoundGlobalScope
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public FunctionSymbol? MainFunction { get; private set; }
        public ImmutableArray<VariableSymbol> Variables { get; private set; }
        public ImmutableArray<FunctionSymbol> Functions { get; private set; }
        public ImmutableArray<BoundStatement> Statements { get; private set; }

        public BoundGlobalScope(ImmutableArray<Diagnostic> diagnostics, FunctionSymbol? mainFunction, ImmutableArray<VariableSymbol> variables, ImmutableArray<FunctionSymbol> functions, ImmutableArray<BoundStatement> statements)
        {
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Variables = variables;
            Functions = functions;
            Statements = statements;
        }
    }
}