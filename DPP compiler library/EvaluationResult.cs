using DPP_Compiler.Diagnostics;
using System.Collections.Immutable;

namespace DPP_Compiler
{
    public sealed class EvaluationResult
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public object? Value { get; private set; }

        public EvaluationResult(ImmutableArray<Diagnostic> diagnostics, object? value)
        {
            Diagnostics = diagnostics;
            Value = value;
        }
    }
}
