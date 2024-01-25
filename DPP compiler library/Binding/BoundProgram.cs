using DPP_Compiler.Diagnostics;
using DPP_Compiler.Symbols;
using System.Collections.Immutable;

namespace DPP_Compiler.Binding
{
    internal class BoundProgram
    {
        public BoundGlobalScope GlobalScope { get; private set; }
        public DiagnosticBag Diagnostics { get; private set; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> FunctionBodies { get; private set; }

        public BoundProgram(BoundGlobalScope globalScope, DiagnosticBag diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> immutableDictionary)
        {
            GlobalScope = globalScope;
            Diagnostics = diagnostics;
            FunctionBodies = immutableDictionary;
        }
    }
}