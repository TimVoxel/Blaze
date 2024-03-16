using Blaze.Diagnostics;
using Blaze.Symbols;
using System.Collections.Immutable;

namespace Blaze.Binding
{
    internal class BoundProgram
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public FunctionSymbol? MainFunction { get; private set; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; private set; }

        public BoundProgram(ImmutableArray<Diagnostic> diagnostics, FunctionSymbol? mainFunction, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
        {
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            Functions = functions;
        }
    }
}