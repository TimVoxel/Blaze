using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal class BoundProgram
    {
        public BoundProgram? Previous { get; private set; }
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; private set; }
        public BoundBlockStatement Statement { get; private set; }

        public BoundProgram(BoundProgram? previous, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, BoundBlockStatement statement)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Functions = functions;
            Statement = statement;
        }
    }
}