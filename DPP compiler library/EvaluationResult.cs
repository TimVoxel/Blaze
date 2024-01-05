using DPP_Compiler.Diagnostics;

namespace DPP_Compiler
{
    public sealed class EvaluationResult
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; private set; }
        public object? Value { get; private set; }

        public EvaluationResult(IReadOnlyList<Diagnostic> diagnostics, object? value)
        {
            Diagnostics = diagnostics;
            Value = value;
        }
    }
}
