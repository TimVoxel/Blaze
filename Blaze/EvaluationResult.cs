using Blaze.Diagnostics;
using System.Collections.Immutable;

namespace Blaze
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
