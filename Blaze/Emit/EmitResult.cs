using Blaze.Diagnostics;
using System.Collections.Immutable;

namespace Blaze.Emit
{
    public sealed class EmitResult
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        public EmitResult(ImmutableArray<Diagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
        }
    }
}
