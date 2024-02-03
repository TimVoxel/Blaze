using DPP_Compiler.Diagnostics;
using DPP_Compiler.Symbols;
using System.Collections.Immutable;

namespace DPP_Compiler.Binding
{
    internal class BoundProgram
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; private set; }
        public BoundBlockStatement Statement { get; private set; }

        public BoundProgram(ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, BoundBlockStatement statement)
        {
            Diagnostics = diagnostics;
            Functions = functions;
            Statement = statement;
        }
    }
}